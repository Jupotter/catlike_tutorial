﻿using System.Collections.Generic;

public class HexCellPriorityQueue
{
    int count   = 0;
    int minimum = int.MaxValue;

    public int Count
    {
        get { return count; }
    }

    List<HexCell> list = new List<HexCell>();

    public void Enqueue(HexCell cell)
    {
        this.count += 1;
        int priority = cell.SearchPriority;

        if (priority < minimum) {
            minimum = priority;
        }

        while (priority >= list.Count) {
            list.Add(null);
        }

        cell.NextWithSamePriority = list[priority];
        list[priority]            = cell;
    }

    public HexCell Dequeue()
    {
        this.count -= 1;

        for (; minimum < list.Count; minimum++) {
            HexCell cell = list[minimum];

            if (cell == null) {
                continue;
            }

            this.list[minimum] = cell.NextWithSamePriority;

            return cell;
        }

        return null;
    }

    public void Change(HexCell cell, int oldPriority)
    {
        HexCell current = list[oldPriority];
        HexCell next    = current.NextWithSamePriority;

        if (current == cell) {
            list[oldPriority] = next;
        } else {
            while (next != cell) {
                current = next;
                next    = current.NextWithSamePriority;
            }
        }

        current.NextWithSamePriority = cell.NextWithSamePriority;
        Enqueue(cell);
        count -= 1;
    }

    public void Clear()
    {
        this.count = 0;
        list.Clear();
        minimum = int.MaxValue;
    }
}
