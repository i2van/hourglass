﻿#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// ReSharper disable all

namespace KPreisser.UI;

/// <summary>
/// 
/// </summary>
public class TaskDialogRadioButtonCollection
    : Collection<TaskDialogRadioButton>
{
    // HashSet to detect duplicate items.
    private readonly HashSet<TaskDialogRadioButton> _itemSet =
        [];

    /// <summary>
    /// 
    /// </summary>
    public TaskDialogRadioButtonCollection()
    {
    }

    internal TaskDialogPage BoundPage { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public TaskDialogRadioButton Add(string text)
    {
        var button = new TaskDialogRadioButton()
        {
            Text = text
        };

        Add(button);
        return button;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    protected override void SetItem(int index, TaskDialogRadioButton item)
    {
        // Disallow collection modification, so that we don't need to copy it
        // when binding the TaskDialogPage.
        BoundPage?.DenyIfBound();
        DenyIfHasOtherCollection(item);

        TaskDialogRadioButton oldItem = this[index];
        if (oldItem != item)
        {
            // First, add the new item (which will throw if it is a duplicate entry),
            // then remove the old one.
            if (!_itemSet.Add(item))
                throw new ArgumentException($"Item {item.Text} is already present", nameof(item));
            _itemSet.Remove(oldItem);

            oldItem.Collection = null;
            item.Collection = this;
        }

        base.SetItem(index, item);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    protected override void InsertItem(int index, TaskDialogRadioButton item)
    {
        // Disallow collection modification, so that we don't need to copy it
        // when binding the TaskDialogPage.
        BoundPage?.DenyIfBound();
        DenyIfHasOtherCollection(item);

        if (!_itemSet.Add(item))
            throw new ArgumentException($"Item {item.Text} is already present", nameof(item));

        item.Collection = this;
        base.InsertItem(index, item);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    protected override void RemoveItem(int index)
    {
        // Disallow collection modification, so that we don't need to copy it
        // when binding the TaskDialogPage.
        BoundPage?.DenyIfBound();

        TaskDialogRadioButton oldItem = this[index];
        oldItem.Collection = null;
        _itemSet.Remove(oldItem);
        base.RemoveItem(index);
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void ClearItems()
    {
        // Disallow collection modification, so that we don't need to copy it
        // when binding the TaskDialogPage.
        BoundPage?.DenyIfBound();

        foreach (TaskDialogRadioButton button in this)
            button.Collection = null;

        _itemSet.Clear();
        base.ClearItems();
    }

    private void DenyIfHasOtherCollection(TaskDialogRadioButton item)
    {
        if (item.Collection != null && item.Collection != this)
            throw new InvalidOperationException(
                "This control is already part of a different collection.");
    }
}