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
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Desktop.AddIns;
using ESRI.ArcGIS.Controls;
using CoordinateConversionLibrary.ViewModels;

namespace ArcMapAddinCoordinateConversion
{
    public class CoordinateConversionButton : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        public CoordinateConversionButton()
        {
        }

        protected override void OnClick()
        {
            ArcMap.Application.CurrentTool = null;

            UID dockWinID = new UIDClass();
            dockWinID.Value = ThisAddIn.IDs.DockableWindowCoordinateConversion;

            IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
            dockWindow.Show(true);
        }
        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }

    public class PointTool : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        ISnappingEnvironment m_SnappingEnv;
        IPointSnapper m_Snapper;
        ISnappingFeedback m_SnappingFeedback;

        public PointTool()
        {
        }

        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }

        protected override void OnActivate()
        {
            //Get the snap environment and initialize the feedback
            UID snapUID = new UID();

            snapUID.Value = "{E07B4C52-C894-4558-B8D4-D4050018D1DA}";
            m_SnappingEnv = ArcMap.Application.FindExtensionByCLSID(snapUID) as ISnappingEnvironment;
            m_Snapper = m_SnappingEnv.PointSnapper;
            m_SnappingFeedback = new SnappingFeedbackClass();
            m_SnappingFeedback.Initialize(ArcMap.Application, m_SnappingEnv, true);
        }

        protected override void OnMouseDown(ESRI.ArcGIS.Desktop.AddIns.Tool.MouseEventArgs arg)
        {
            if (arg.Button != System.Windows.Forms.MouseButtons.Left)
                return;
            try
            {
                var point = GetMapPoint(arg.X, arg.Y);
                ISnappingResult snapResult = null;
                //Try to snap the current position
                snapResult = m_Snapper.Snap(point);
                m_SnappingFeedback.Update(null, 0);
                if (snapResult != null && snapResult.Location != null)
                    point = snapResult.Location;

                var doc = AddIn.FromID<ArcMapAddinCoordinateConversion.DockableWindowCoordinateConversion.AddinImpl>(ThisAddIn.IDs.DockableWindowCoordinateConversion);

                if (doc != null && point != null)
                {
                    doc.GetMainVM().IsToolGenerated = true;
                    doc.SetInput(point.X, point.Y);
                }

                doc.GetMainVM().IsToolActive = false;
            }
            catch { }
        }

        protected override void OnMouseMove(MouseEventArgs arg)
        {
            try
            {
                IPoint point = GetMapPoint(arg.X, arg.Y);
                ISnappingResult snapResult = null;
                //Try to snap the current position
                snapResult = m_Snapper.Snap(point);
                m_SnappingFeedback.Update(snapResult, 0);
                if (snapResult != null && snapResult.Location != null)
                    point = snapResult.Location;

                var doc = AddIn.FromID<ArcMapAddinCoordinateConversion.DockableWindowCoordinateConversion.AddinImpl>(ThisAddIn.IDs.DockableWindowCoordinateConversion);

                if (doc != null && point != null)
                {
                    doc.GetMainVM().IsHistoryUpdate = false;
                    doc.SetInput(point.X, point.Y);
                }
            }
            catch { }
        }

        private IPoint GetMapPoint(int X, int Y)
        {
            //Get the active view from the ArcMap static class.
            IActiveView activeView = ArcMap.Document.FocusMap as IActiveView;

            var point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y) as IPoint;

            if (CoordinateConversionViewModel.AddInConfig.DisplayCoordinateType == CoordinateConversionLibrary.CoordinateTypes.None)
            {
                //IActiveView activeView = ArcMap.Document.FocusMap as IActiveView;
                point.SpatialReference = ArcMap.Document.FocusMap.SpatialReference;
            }
            else
            {
                // always use WGS84
                var sr = GetSR();

                if (sr != null)
                {
                    point.Project(sr);
                }
            }

            return point;
        }

        private ISpatialReference GetSR()
        {
            Type t = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
            System.Object obj = Activator.CreateInstance(t);
            ISpatialReferenceFactory srFact = obj as ISpatialReferenceFactory;

            // Use the enumeration to create an instance of the predefined object.

            IGeographicCoordinateSystem geographicCS =
                srFact.CreateGeographicCoordinateSystem((int)
                esriSRGeoCSType.esriSRGeoCS_WGS1984);

            return geographicCS as ISpatialReference;
        }

    }

}
