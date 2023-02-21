﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace JWLMerge.Models
{
    using System.Windows.Media;

    internal sealed class ColourListItem : ObservableObject
    {
        private bool _isChecked;

        public ColourListItem(string name, int id, Color color)
        {
            Name = name;
            Id = id;
            Color = color;
        }

        public string Name { get; }

        public int Id { get; }

        public Color Color { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public override string ToString() => Name;
    }
}
