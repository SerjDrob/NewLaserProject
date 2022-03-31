﻿using MachineClassLibrary.Classes;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using NewLaserProject.Properties;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace NewLaserProject.ViewModels;

internal partial class MainViewModel
{
    public int FileScale { get; set; } = 1000;
    public bool MirrorX { get; set; } = true;
    public bool WaferTurn90 { get; set; } = false;
    public double WaferOffsetX { get; set; }
    public double WaferOffsetY { get; set; }
    public double WaferWidth { get; set; } = 48;
    public double WaferHeight { get; set; } = 60;
    public double WaferMargin { get; set; } = 0.2;
    public double FileSizeX { get; set; }
    public double FileSizeY { get; set; }
    public double FieldSizeX { get => /*FileScale * WaferWidth*/60000; }
    public double FieldSizeY { get => /*FileScale * WaferHeight*/48000; }
    public bool WaferContourVisibility { get; set; } = true;
    public bool IsFileSettingsEnable { get; set; } = false;
    public string FileName { get; set; } = "open new file";
    public ObservableCollection<LayerGeometryCollection> LayGeoms { get; set; } = new();

    private IDxfReader _dxfReader;

    [ICommand]
    private void AlignWafer(object obj)
    {
        var param = (ValueTuple<object, object, object>)obj;
        var scaleX = (double)param.Item1;
        var scaleY = (double)param.Item2;
        var aligning = (Aligning)param.Item3;
        var size = _dxfReader.GetSize();
        var dx = WaferWidth * FileScale;
        var dy = WaferHeight * FileScale;

        WaferOffsetX = aligning switch
        {
            Aligning.Right or Aligning.RTCorner or Aligning.RBCorner => -(WaferTurn90 ? (size.height - dx) : (size.width - dx)) * scaleX / 2,
            Aligning.Left or Aligning.LTCorner or Aligning.LBCorner => (WaferTurn90 ? (size.height - dx) : (size.width - dx)) * scaleX / 2,
            Aligning.Top or Aligning.Bottom or Aligning.Center => 0,
        };

        WaferOffsetY = aligning switch
        {
            Aligning.Top or Aligning.RTCorner or Aligning.LTCorner => (WaferTurn90 ? (size.width - dy) : (size.height - dy)) * scaleY / 2,
            Aligning.Bottom or Aligning.RBCorner or Aligning.LBCorner => -(WaferTurn90 ? (size.width - dy) : (size.height - dy)) * scaleY / 2,
            Aligning.Right or Aligning.Left or Aligning.Center => 0
        };

    }

    [ICommand]
    private void OpenFile()
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.InitialDirectory = "d:\\";
        openFileDialog.Filter = "dxf files (*.dxf)|*.dxf";
        openFileDialog.FilterIndex = 2;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() ?? false)
        {
            //techMessager.RealeaseMessage("Загрузка...", Icon.Loading);

            //Get the path of specified file
            FileName = openFileDialog.FileName;
            if (File.Exists(FileName))
            {
                _dxfReader = new IMDxfReader(FileName);
                var fileSize = _dxfReader.GetSize();
                FileSizeX = Math.Round(fileSize.width);
                FileSizeY = Math.Round(fileSize.height);
                LayGeoms = new LayGeomAdapter(_dxfReader).LayerGeometryCollections;
                IsFileSettingsEnable = true;
                LPModel = new(_dxfReader);
                TWModel = new();

                MirrorX = Settings.Default.WaferMirrorX;
                WaferTurn90 = Settings.Default.WaferAngle90;
                WaferOffsetX = 0;
                WaferOffsetY = 0;

                LPModel.ObjectChosenEvent += TWModel.SetObjectsTC;
            }
            else
            {
                IsFileSettingsEnable = false;
            }
           // techMessager.EraseMessage();
        }

    }


}
