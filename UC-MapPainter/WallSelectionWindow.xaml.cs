using System;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;

namespace UC_MapPainter
{
    public partial class WallSelectionWindow : Window
    {
        private DFacet _selectedFacet;
        private List<short> _dstyles;
        private int _selectedFacetDStyleIndex;

        public WallSelectionWindow()
        {
            InitializeComponent();
            this.Loaded += WallSelectionWindow_Loaded;
        }

        private void WallSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize window logic here if needed
            // Populate the ComboBox with enum values
            FacetTypeComboBox.ItemsSource = Enum.GetValues(typeof(FacetType))
                                                .Cast<FacetType>()
                                                .Select(ft => new { Name = ft.ToString(), Value = (int)ft })
                                                .ToList();

            // Optionally set default selection
            FacetTypeComboBox.SelectedIndex = 0;
        }

        public void SetSelectedFacet(DFacet facet, int facetDStyleIndex, List<short> dstyles)
        {
            _dstyles = dstyles;
            _selectedFacet = facet;
            _selectedFacetDStyleIndex = facetDStyleIndex;
            UpdateCheckboxes();
            UpdateTextFields();
            UpdateComboboxFields();
        }

        private void UpdateCheckboxes()
        {
            // Update the state of checkboxes based on _selectedFacet flags
            if (_selectedFacet != null)
            {
                CheckboxInvisible.IsChecked = _selectedFacet.IsInvisible();
                CheckboxInside.IsChecked = _selectedFacet.IsInside();
                CheckboxDlit.IsChecked = _selectedFacet.IsDlit();
                CheckboxHugFloor.IsChecked = _selectedFacet.IsHugFloor();
                CheckboxElectrified.IsChecked = _selectedFacet.IsElectrified();
                CheckboxTwoSided.IsChecked = _selectedFacet.IsTwoSided();
                CheckboxUnclimbable.IsChecked = _selectedFacet.IsUnclimbable();
                CheckboxOnBuilding.IsChecked = _selectedFacet.IsOnBuilding();
                CheckboxBarbTop.IsChecked = _selectedFacet.IsBarbTop();
                CheckboxSeeThrough.IsChecked = _selectedFacet.IsSeeThrough();
                CheckboxOpen.IsChecked = _selectedFacet.IsOpen();
                Checkbox90Degree.IsChecked = _selectedFacet.Is90Degree();
                CheckboxTwoTextured.IsChecked = _selectedFacet.IsTwoSided();
                CheckboxFenceCut.IsChecked = _selectedFacet.IsFenceCut();
                // Add other flag checks here
            }
        }

        private void UpdateTextFields()
        {
            BlockHeightBox.Text = _selectedFacet.BlockHeight.ToString();
            DStyleBox.Text = _dstyles[_selectedFacetDStyleIndex].ToString();
            YBox.Text = _selectedFacet.Y[0].ToString();
        }

        private void UpdateComboboxFields()
        {
            if (_selectedFacet != null && FacetTypeComboBox.ItemsSource is IEnumerable<dynamic> items)
            {
                // Find the item in the ComboBox that matches the selected FacetType value
                var matchingItem = items.FirstOrDefault(item => item.Value == _selectedFacet.FacetType);

                // Set the selected item in the ComboBox
                if (matchingItem != null)
                {
                    FacetTypeComboBox.SelectedItem = matchingItem;
                }
            }
        }

        //private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (_selectedFacet != null)
        //    {
        //        if (sender == CheckboxInvisible) _selectedFacet.UnsetInvisible();
        //        else if (sender == CheckboxInside) _selectedFacet.UnsetInside();
        //        else if (sender == CheckboxDlit) _selectedFacet.UnsetDlit();
        //        else if (sender == CheckboxUnclimbable) _selectedFacet.UnsetUnclimbable();
        //        else if (sender == CheckboxSeeThrough) _selectedFacet.UnsetSeeThrough();
        //        // Add other flags
        //    }
        //}

        public ushort GetFlags()
        {
            ushort flags = 0;

            // Fixme if you want to use it
            // Some offsets are wrong as it should not be ordered 0,1,2,3... 
            if (CheckboxInvisible.IsChecked == true) flags |= 1 << 0;
            if (CheckboxInside.IsChecked == true) flags |= 1 << 1;
            if (CheckboxDlit.IsChecked == true) flags |= 1 << 2;
            if (CheckboxHugFloor.IsChecked == true) flags |= 1 << 3;
            if (CheckboxElectrified.IsChecked == true) flags |= 1 << 4;
            if (CheckboxTwoSided.IsChecked == true) flags |= 1 << 5;
            if (CheckboxUnclimbable.IsChecked == true) flags |= 1 << 6;
            if (CheckboxOnBuilding.IsChecked == true) flags |= 1 << 7;
            if (CheckboxBarbTop.IsChecked == true) flags |= 1 << 8;
            if (CheckboxSeeThrough.IsChecked == true) flags |= 1 << 9;
            if (CheckboxOpen.IsChecked == true) flags |= 1 << 10;
            if (Checkbox90Degree.IsChecked == true) flags |= 1 << 11;
            if (CheckboxTwoTextured.IsChecked == true) flags |= 1 << 12;
            if (CheckboxFenceCut.IsChecked == true) flags |= 1 << 13;

            return flags;
        }


        private void Invisible_Click(object sender, RoutedEventArgs e)
        {
            if (CheckboxInvisible.IsChecked == true)
            {
                _selectedFacet.SetInvisible();
                // CheckboxInvisible.Text = "Checked";
            }
            else
            {
                _selectedFacet.UnsetInvisible();
                //checkBox1.Text = "Unchecked";
            }
        }
        //private void Checkbox_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (_selectedFacet != null)
        //    {
        //        if (sender == CheckboxInvisible) _selectedFacet.SetInvisible();
        //        else if (sender == CheckboxInside) _selectedFacet.SetInside();
        //        else if (sender == CheckboxDlit) _selectedFacet.SetDlit();
        //        else if (sender == CheckboxUnclimbable) _selectedFacet.SetUnclimbable();
        //        else if (sender == CheckboxSeeThrough) _selectedFacet.SetSeeThrough();
        //        // Add other flags
        //    }
        //}


        private void Inside_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxInside.IsChecked == true)
            {
                _selectedFacet.SetInside();
            }
            else
            {  _selectedFacet.UnsetInside();}
        }

        private void Dlit_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxDlit.IsChecked == true)
            {
                _selectedFacet.SetDlit();
            }
            else { _selectedFacet.UnsetDlit();}
        }

        private void HugFloor_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxHugFloor.IsChecked == true)
            {
                _selectedFacet.SetHugFloor();
            }
            else
            {
                _selectedFacet.UnsetHugFloor();
            }
        }
        private void Electrified_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxElectrified.IsChecked == true)
            {
                _selectedFacet.SetElectrified();
            }
            else
            {
                _selectedFacet.UnsetElectrified();
            }
        }
        private void TwoSided_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxTwoSided.IsChecked == true)
            {
                _selectedFacet.SetTwoSided();
            }
            else
            {
                _selectedFacet.UnsetTwoSided();
            }
        }
        private void Unclimbable_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxUnclimbable.IsChecked == true) 
            {
                _selectedFacet.SetUnclimbable();
            }
            else
            {
                _selectedFacet.UnsetUnclimbable();
            }
        }
        private void OnBuilding_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxOnBuilding.IsChecked == true)
            {
                _selectedFacet.SetOnBuilding();
            }
            else
            {
                _selectedFacet.UnsetOnBuilding();
            }
        }
        private void BarbTop_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxBarbTop.IsChecked == true)
            {
                _selectedFacet.SetBarbTop();
            }
            else
            {
                _selectedFacet.UnsetBarbTop();
            }
        }
        private void SeeThrough_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxSeeThrough.IsChecked == true)
            {
                _selectedFacet.SetSeeThrough();
            }
            else
            {
                _selectedFacet.UnsetSeeThrough();
            }

        }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxOpen.IsChecked == true)
            {
                _selectedFacet.SetOpen();
            }
            else
            {
                _selectedFacet.UnsetOpen();
            }
        }
        private void NinetyDegree_Click(object sender, RoutedEventArgs e)
        {
            if(Checkbox90Degree.IsChecked == true)
            {
                _selectedFacet.Set90Degree();
            }
            else
            {
                _selectedFacet.Unset90Degree();
            }
        }
        private void TwoTextured_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxTwoSided.IsChecked == true)
            {
                _selectedFacet.SetTwoTextured();
            }
            else
            { 
                _selectedFacet.UnsetTwoTextured();
            }
        }
        private void FenceCut_Click(object sender, RoutedEventArgs e)
        {
            if(CheckboxFenceCut.IsChecked == true)
            {
                _selectedFacet.SetFenceCut();
            }
            else
            {
                _selectedFacet.UnsetFenceCut();
            }
        }

        private void BlockHeightBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (byte.TryParse(BlockHeightBox.Text, out byte value))
            {
                _selectedFacet.BlockHeight = value;
                //height = value;
                //HeightSlider.Value = height;
                //UpdateStoreyLabel();
            }
        }
        private void FacetTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FacetTypeComboBox.SelectedItem is not null)
            {
                var selectedItem = (dynamic)FacetTypeComboBox.SelectedItem;
                string selectedName = selectedItem.Name;
                int selectedValue = selectedItem.Value;


                if (_selectedFacet != null)
                {
                    _selectedFacet.FacetType = (byte)selectedValue;
                }

                // Use the selected name and value
                //MessageBox.Show($"Selected: {selectedName} (Value: {selectedValue})");
            }
        }

        private void DStyle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(DStyleBox.Text, out short value))
            {
                _dstyles[_selectedFacetDStyleIndex] = value;
            }
        }

        private void YBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(YBox.Text, out short value))
            {
                _selectedFacet.Y[0] = value;
                _selectedFacet.Y[1] = value;
            }
        }
    }
}