﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public static HexUnit unitPrefab;

    const float rotationSpeed = 180f;
    const float travelSpeed   = 4f;

    private float         orientation;
    private HexCell       location;
    private List<HexCell> pathToTravel;

    public HexCell Location
    {
        get { return location; }
        set
        {
            if (location) {
                location.Unit = null;
            }

            location                = value;
            value.Unit              = this;
            transform.localPosition = value.Position;
        }
    }

    public float Orientation
    {
        get { return orientation; }
        set
        {
            orientation             = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation   = Quaternion.LookRotation(point - transform.localPosition);

        float angle = Quaternion.Angle(fromRotation, toRotation);
        float speed = rotationSpeed / angle;

        if (angle > 0f) {
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed) {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);

                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }

    IEnumerator TravelPath()
    {
        Vector3 a, b, c = pathToTravel[0].Position;

        transform.localPosition = c;
        yield return LookAt(pathToTravel[1].Position);

        float t = Time.deltaTime * travelSpeed;

        for (int i = 1; i < pathToTravel.Count; i++) {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * 0.5f;

            for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y                     = 0f;
                transform.localRotation = Quaternion.LookRotation(d);

                yield return null;
            }

            t -= 1f;
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;

        for (; t < 1f; t += Time.deltaTime * travelSpeed) {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y                     = 0f;
            transform.localRotation = Quaternion.LookRotation(d);

            yield return null;
        }

        transform.localPosition = location.Position;
        orientation             = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }

    void OnEnable()
    {
        if (location) {
            transform.localPosition = location.Position;
        }
    }

    void OnDrawGizmos()
    {
        if (pathToTravel == null || pathToTravel.Count == 0) {
            return;
        }

        Gizmos.color = Color.blue;

        Vector3 a, b, c = pathToTravel[0].Position + Vector3.up;

        Vector3 start;
        Vector3 end;

        for (int i = 1; i < pathToTravel.Count; i++) {
            a = c;
            b = pathToTravel[i - 1].Position          + Vector3.up;
            c = (b + pathToTravel[i].Position) * 0.5f + Vector3.up;

            start = a;
            end   = b;

            for (float t = 0f; t < 1f; t += 0.1f) {
                end = Bezier.GetPoint(a, b, c, t);
                Gizmos.DrawLine(start, end);
                start = end;
            }

            Gizmos.DrawLine(end, c);
        }

        a     = c;
        b     = pathToTravel[pathToTravel.Count - 1].Position + Vector3.up;
        start = a;
        end   = b;

        for (float t = 0f; t < 1f; t += 0.1f) {
            end = Bezier.GetPoint(a, b, c, t);
            Gizmos.DrawLine(start, end);
            start = end;
        }

        Gizmos.DrawLine(end, b);
    }

    public void Die()
    {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;
    }

    public void Travel(List<HexCell> path)
    {
        Location     = path[path.Count - 1];
        pathToTravel = path;

        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    public void Save(BinaryWriter writer)
    {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float          orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
    }
}
