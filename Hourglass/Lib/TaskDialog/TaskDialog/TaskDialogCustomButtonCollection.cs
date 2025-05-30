﻿#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// ReSharper disable all

namespace KPreisser.UI;

/// <summary>
/// 
/// </summary>
public class TaskDialogCustomButtonCollection
    : Collection<TaskDialogCustomButton>
{
    // HashSet to detect duplicate items.
    private readonly HashSet<TaskDialogCustomButton> _itemSet =
        [];

    /// <summary>
    /// 
    /// </summary>
    public TaskDialogCustomButtonCollection()
    {
    }

    internal TaskDialogPage BoundPage { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="descriptionText"></param>
    /// <returns></returns>
    public TaskDialogCustomButton Add(string text, string descriptionText = null)
    {
        var button = new TaskDialogCustomButton()
        {
            Text = text,
            DescriptionText = descriptionText
        };

        Add(button);
        return button;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    protected override void SetItem(int index, TaskDialogCustomButton item)
    {
        // Disallow collection modification, so that we don't need to copy it
        // when binding the TaskDialogPage.
        BoundPage?.DenyIfBound();
        DenyIfHasOtherCollection(item);

        TaskDialogCustomButton oldItem = this[index];
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
    protected override void InsertItem(int index, TaskDialogCustomButton item)
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

        TaskDialogCustomButton oldItem = this[index];
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

        foreach (TaskDialogCustomButton button in this)
            button.Collection = null;

        _itemSet.Clear();
        base.ClearItems();
    }

    private void DenyIfHasOtherCollection(TaskDialogCustomButton item)
    {
        if (item.Collection != null && item.Collection != this)
            throw new InvalidOperationException(
                "This control is already part of a different collection.");
    }
}