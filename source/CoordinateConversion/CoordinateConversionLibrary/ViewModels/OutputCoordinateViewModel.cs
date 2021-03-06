﻿/******************************************************************************* 
  * Copyright 2015 Esri 
  *  
  *  Licensed under the Apache License, Version 2.0 (the "License"); 
  *  you may not use this file except in compliance with the License. 
  *  You may obtain a copy of the License at 
  *  
  *  http://www.apache.org/licenses/LICENSE-2.0 
  *   
  *   Unless required by applicable law or agreed to in writing, software 
  *   distributed under the License is distributed on an "AS IS" BASIS, 
  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
  *   See the License for the specific language governing permissions and 
  *   limitations under the License. 
  ******************************************************************************/ 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using CoordinateConversionLibrary.Models;
using CoordinateConversionLibrary.Helpers;
using CoordinateConversionLibrary.Views;
using System.Windows;

namespace CoordinateConversionLibrary.ViewModels
{
    public class OutputCoordinateViewModel : BaseViewModel
    {
        public OutputCoordinateViewModel() 
        {
            ConfigCommand = new RelayCommand(OnConfigCommand);
            ExpandCommand = new RelayCommand(OnExpandCommand);
            DeleteCommand = new RelayCommand(OnDeleteCommand);
            CopyCommand = new RelayCommand(OnCopyCommand);

            Mediator.Register(CoordinateConversionLibrary.Constants.AddNewOutputCoordinate, OnAddNewOutputCoordinate);
            Mediator.Register(CoordinateConversionLibrary.Constants.CopyAllCoordinateOutputs, OnCopyAllCoordinateOutputs);

            //for testing without a config file, init a few sample items
            //OutputCoordinateList = new ObservableCollection<OutputCoordinateModel>();
            ////var tempProps = new Dictionary<string, string>() { { "Lat", "70.49N" }, { "Lon", "40.32W" } };
            ////var mgrsProps = new Dictionary<string, string>() { { "GZone", "17T" }, { "GSquare", "NE" }, { "Northing", "86309" }, { "Easting", "77770" } };
            //OutputCoordinateList.Add(new OutputCoordinateModel { Name = "DD", CType = CoordinateType.DD, OutputCoordinate = "70.49N 40.32W" });
            //OutputCoordinateList.Add(new OutputCoordinateModel { Name = "DMS", CType = CoordinateType.DMS, OutputCoordinate = "40°26'46\"N,79°58'56\"W", Format = "A0°B0'C0\"N X0°Y0'Z0\"E" });
            //OutputCoordinateList.Add(new OutputCoordinateModel { Name = "MGRS", CType = CoordinateType.MGRS, OutputCoordinate = @"", Format = "Z S X00000 Y00000" });
            //OutputCoordinateList.Add(new OutputCoordinateModel { Name = "UTM", CType = CoordinateType.UTM, OutputCoordinate = @"", Format = "Z#H X0 Y0" });
            //OutputCoordinateList.Add(new OutputCoordinateModel { Name = "GARS", CType = CoordinateType.GARS, OutputCoordinate = @"", Format = "X#YQK" });
            //OutputCoordinateList.Add(new OutputCoordinateModel { Name = "USNG", CType = CoordinateType.USNG, OutputCoordinate = @"", Format = "Z S X0 Y0" });
            //OutputCoordinateList.Add(new OutputCoordinateModel { Name = "DDM", CType = CoordinateType.DDM, OutputCoordinate = @"", Format = "A0 B0.0### N X0 Y0.0### E" });

            //DefaultFormatList = new ObservableCollection<DefaultFormatModel>();

            //DefaultFormatList.Add(new DefaultFormatModel { CType = CoordinateType.DD, DefaultNameFormatDictionary = new SerializableDictionary<string, string> { { "70.49N 40.32W", "Y0.0#N X0.0#E" }, { "70.49N,40.32W", "Y0.0#N,X0.0#E" } } });

            configObserver = new PropertyObserver<CoordinateConversionLibraryConfig>(CoordinateConversionViewModel.AddInConfig)
            .RegisterHandler(n => n.OutputCoordinateList, n=> RaisePropertyChanged(()=> OutputCoordinateList));
        }

        PropertyObserver<CoordinateConversionLibraryConfig> configObserver;

        private void OnCopyAllCoordinateOutputs(object obj)
        {
            var sb = new StringBuilder();

            string inputCoordinate = obj as string;

            if (!string.IsNullOrWhiteSpace(inputCoordinate))
                sb.AppendLine(inputCoordinate);

            foreach (var output in CoordinateConversionViewModel.AddInConfig.OutputCoordinateList)
            {
                sb.AppendLine(output.OutputCoordinate);
            }

            if(sb.Length > 0)
            {
                // copy to clipboard
                System.Windows.Clipboard.SetText(sb.ToString());
            }
        }

        private void OnAddNewOutputCoordinate(object obj)
        {
            var outputCoordItem = obj as OutputCoordinateModel;

            if (outputCoordItem == null)
                return;

            var dlg = new EditOutputCoordinateView(CoordinateConversionViewModel.AddInConfig.DefaultFormatList, GetInUseNames(), new OutputCoordinateModel() { CType = outputCoordItem.CType, Format = outputCoordItem.Format, Name = outputCoordItem.Name, SRName = outputCoordItem.SRName, SRFactoryCode = outputCoordItem.SRFactoryCode });

            var vm = dlg.DataContext as EditOutputCoordinateViewModel;
            vm.WindowTitle = Properties.Resources.TitleAddNewOutputCoordinate;
            
            if (dlg.ShowDialog() == true)
            {
                outputCoordItem.Format = vm.Format;

                CoordinateType type;
                if (Enum.TryParse<CoordinateType>(vm.CategorySelection, out type))
                {
                    outputCoordItem.CType = type;
                }

                outputCoordItem.Name = vm.OutputCoordItem.Name;
                outputCoordItem.SRFactoryCode = vm.OutputCoordItem.SRFactoryCode;
                outputCoordItem.SRName = vm.OutputCoordItem.SRName;

                CoordinateConversionViewModel.AddInConfig.OutputCoordinateList.Add(outputCoordItem);
                Mediator.NotifyColleagues(CoordinateConversionLibrary.Constants.RequestOutputUpdate, null);
                CoordinateConversionViewModel.AddInConfig.SaveConfiguration();
            }
        }

        private List<string> GetInUseNames()
        {
            return CoordinateConversionViewModel.AddInConfig.OutputCoordinateList.Select(oc => oc.Name).ToList();
        }

        /// <summary>
        /// The bound list.
        /// </summary>
        public ObservableCollection<OutputCoordinateModel> OutputCoordinateList
        {
            get { return CoordinateConversionViewModel.AddInConfig.OutputCoordinateList; }
        }

        #region relay commands
        [XmlIgnore]
        public RelayCommand DeleteCommand { get; set; }
        [XmlIgnore]
        public RelayCommand ConfigCommand { get; set; }
        [XmlIgnore]
        public RelayCommand ExpandCommand { get; set; }
        [XmlIgnore]
        public RelayCommand CopyCommand { get; set; }

        // copy parameter to clipboard
        private void OnCopyCommand(object obj)
        {
            var coord = obj as string;

            if(!string.IsNullOrWhiteSpace(coord))
            {
                // copy to clipboard
                System.Windows.Clipboard.SetText(coord);
            }
        }

        private void OnDeleteCommand(object obj)
        {
            var name = obj as string;

            if (!string.IsNullOrEmpty(name))
            {
                // lets make sure
                if (System.Windows.MessageBoxResult.Yes != System.Windows.MessageBox.Show(string.Format(Properties.Resources.FormattedRemove, name), Properties.Resources.LabelConfirmRemoval, System.Windows.MessageBoxButton.YesNo))
                    return;

                foreach (var item in CoordinateConversionViewModel.AddInConfig.OutputCoordinateList)
                {
                    if (item.Name == name)
                    {
                        CoordinateConversionViewModel.AddInConfig.OutputCoordinateList.Remove(item);
                        CoordinateConversionViewModel.AddInConfig.SaveConfiguration();
                        return;
                    }
                }
            }
        }

        private void OnExpandCommand(object obj)
        {
            var name = obj as string;

            if(!string.IsNullOrWhiteSpace(name))
            {
                foreach (var item in CoordinateConversionViewModel.AddInConfig.OutputCoordinateList)
                {
                    if(item.Name == name)
                    {
                        item.ToggleVisibility();
                        return;
                    }
                }
            }
        }

        private void OnConfigCommand(object obj)
        {
            if (obj == null || string.IsNullOrWhiteSpace(obj as string))
                return;

            var outputCoordItem = GetOCMByName(obj as string);
            var InUseNames = GetInUseNames();
            InUseNames.Remove(outputCoordItem.Name);
            var dlg = new EditOutputCoordinateView(CoordinateConversionViewModel.AddInConfig.DefaultFormatList, InUseNames, 
                new OutputCoordinateModel() { CType = outputCoordItem.CType, 
                    Format = outputCoordItem.Format, 
                    Name = outputCoordItem.Name,
                    SRName = outputCoordItem.SRName,
                    SRFactoryCode = outputCoordItem.SRFactoryCode});

            var vm = dlg.DataContext as EditOutputCoordinateViewModel;
            vm.WindowTitle = Properties.Resources.TitleEditOutputCoordinate;

            if (dlg.ShowDialog() == true)
            {
                outputCoordItem.Name = vm.OutputCoordItem.Name;
                outputCoordItem.Format = vm.Format;
                outputCoordItem.SRFactoryCode = vm.OutputCoordItem.SRFactoryCode;
                outputCoordItem.SRName = vm.OutputCoordItem.SRName;

                CoordinateType type;
                if (Enum.TryParse<CoordinateType>(vm.CategorySelection, out type))
                {
                    outputCoordItem.CType = type;
                }

                Mediator.NotifyColleagues(CoordinateConversionLibrary.Constants.RequestOutputUpdate, null);
            }

            CoordinateConversionViewModel.AddInConfig.SaveConfiguration();
        }

        #endregion

        private OutputCoordinateModel GetOCMByName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                foreach (var item in CoordinateConversionViewModel.AddInConfig.OutputCoordinateList)
                {
                    if (item.Name == name)
                    {
                        return item;
                    }
                }
            }

            return null;
        }
    }
}
