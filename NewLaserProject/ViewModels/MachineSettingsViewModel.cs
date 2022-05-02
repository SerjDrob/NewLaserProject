using FluentValidation;
using FluentValidation.Validators;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Views;
using Newtonsoft.Json.Serialization;
using PropertyChanged;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class MachineSettingsViewModel : INotifyDataErrorInfo
    {
        private readonly double _currentX;
        private readonly double _currentY;
        private readonly double _currentZ;
        private readonly ImplementINotifyDataErrorInfo<MachineSettingsViewModel> _implementINotifyDataErrorInfo;

        public MachineSettingsViewModel(double currentX, double currentY, double currentZ)
        {
            _currentX = currentX;
            _currentY = currentY;
            _currentZ = currentZ;
            _implementINotifyDataErrorInfo = new ImplementINotifyDataErrorInfo<MachineSettingsViewModel>(this, new MachineSettingsValidator());
            _implementINotifyDataErrorInfo.ErrorsChanged += _implementINotifyDataErrorInfo_ErrorsChanged;

        }

        private void _implementINotifyDataErrorInfo_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            ErrorsChanged?.Invoke(sender, e);
        }


        private void CheckErrorsMethod()
        {
            _implementINotifyDataErrorInfo.CheckAllErrors();
        }
        [OnChangedMethod(methodName: nameof(CheckErrorsMethod))]
        public double XVelLow { get; set; }

        [OnChangedMethod(methodName: nameof(CheckErrorsMethod))]
        public double XVelHigh { get; set; }
        public double XVelService { get; set; }
        public double XAcc { get; set; }

        [OnChangedMethod(methodName: nameof(CheckErrorsMethod))]
        public double YVelLow { get; set; }

        [OnChangedMethod(methodName: nameof(CheckErrorsMethod))]
        public double YVelHigh { get; set; }
        public double YVelService { get; set; }
        public double YAcc { get; set; }

        [OnChangedMethod(methodName: nameof(CheckErrorsMethod))]
        public double ZVelLow { get; set; }

        [OnChangedMethod(methodName: nameof(CheckErrorsMethod))]
        public double ZVelHigh { get; set; }
        public double ZVelService { get; set; }
        public double ZAcc { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public double XLeftPoint { get; set; }
        public double YLeftPoint { get; set; }
        public double XRightPoint { get; set; }
        public double YRightPoint { get; set; }
        public double ZCamera { get; set; }
        public double ZLaser { get; set; }
        public double XLoad { get; set; }
        public double YLoad { get; set; }
        public bool HasErrors => _implementINotifyDataErrorInfo.HasErrors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public IEnumerable GetErrors(string? propertyName) => _implementINotifyDataErrorInfo.GetErrors(propertyName);

        [ICommand]
        private void TeachLeftPoint() => (XLeftPoint, YLeftPoint) = (_currentX, _currentY);
        [ICommand]
        private void TeachRightPoint() => (XRightPoint, YRightPoint) = (_currentX, _currentY);
        [ICommand]
        private void TeachZCamera()
        {
            var waferThickness = 0.5;
            var dc = new AskThicknessVM { Thickness = waferThickness };
            new AskThicknesView { DataContext = dc }.ShowDialog();
            waferThickness = dc.Thickness;

            ZCamera = _currentZ + waferThickness;
        }
        [ICommand]
        private void TeachZLaser()
        {
            var waferThickness = 0.5;
            var dc = new AskThicknessVM { Thickness = waferThickness };
            new AskThicknesView { DataContext = dc }.ShowDialog();
            waferThickness = dc.Thickness;

            ZLaser = _currentZ + waferThickness;
        }
        [ICommand]
        private void TeachLoadPoint() => (XLoad, YLoad) = (_currentX, _currentY);
    }

    internal class ImplementINotifyDataErrorInfo<T> : INotifyDataErrorInfo where T : class, INotifyDataErrorInfo
    {
        private readonly T _validObject;
        private readonly IValidator<T> _validator;
        private List<string> _validatableKeys = new();

        public ImplementINotifyDataErrorInfo(T validObject, IValidator<T> validator)
        {
            _validObject = validObject;
            _validator = validator;
            var descriptor = _validator.CreateDescriptor();
            _validatableKeys = descriptor.GetMembersWithValidators().Select(k => k.Key).ToList();
        }

        public bool HasErrors => _validator.Validate(_validObject).Errors.Any();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public IEnumerable GetErrors(string? propertyName) => _validator.Validate(_validObject, options => options.IncludeProperties(propertyName)).Errors;
        public void CheckAllErrors()
        {
            _validatableKeys.ForEach(key => ErrorsChanged?.Invoke(_validObject, new DataErrorsChangedEventArgs(key)));
        }
    }

    internal class MachineSettingsValidator : AbstractValidator<MachineSettingsViewModel>
    {
        const string LESS_THAN = "Значение должно быть меньше чем";
        const string GREATER_THAN = "Значение должно быть больше чем";
        const string VELLOW = "Скорость мал.";
        const string VELHIGH = "Скорость бол.";
        const string NOTVALID = "Значение должно соответствовать формату ddd.dd";
        const string NOTEMPTY = "Значение не должно быть пустым";

        public MachineSettingsValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            #region X rules
            RuleFor(property => property.XVelLow)
                    .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
                    .NotEmpty().WithMessage(NOTEMPTY)
                    .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
                    .LessThanOrEqualTo(10).WithMessage($"{LESS_THAN} 10")
                    .LessThan(p => p.XVelHigh).WithMessage($"{LESS_THAN} {VELHIGH}");

            RuleFor(property => property.XVelHigh)
               .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
               .NotEmpty().WithMessage(NOTEMPTY)
               .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
               .LessThanOrEqualTo(50).WithMessage($"{LESS_THAN} 50")
               .GreaterThan(p => p.XVelLow).WithMessage($"{GREATER_THAN} {VELLOW}");

            RuleFor(property => property.XVelService)
               .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
               .NotEmpty().WithMessage(NOTEMPTY)
               .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
               .LessThanOrEqualTo(50).WithMessage($"{LESS_THAN} 50");

            RuleFor(property => property.XAcc)
               .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
               .NotEmpty().WithMessage(NOTEMPTY)
               .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
               .LessThanOrEqualTo(1000).WithMessage($"{LESS_THAN} 1000");
            #endregion

            #region Y rules
            RuleFor(property => property.YVelLow)
                   .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
                   .NotEmpty().WithMessage(NOTEMPTY)
                   .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
                   .LessThanOrEqualTo(10).WithMessage($"{LESS_THAN} 10")
                   .LessThan(p => p.YVelHigh).WithMessage($"{LESS_THAN} {VELHIGH}");

            RuleFor(property => property.YVelHigh)
               .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
               .NotEmpty().WithMessage(NOTEMPTY)
               .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
               .LessThanOrEqualTo(50).WithMessage($"{LESS_THAN} 50")
               .GreaterThan(p => p.YVelLow).WithMessage($"{GREATER_THAN} {VELLOW}");

            RuleFor(property => property.YVelService)
               .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
               .NotEmpty().WithMessage(NOTEMPTY)
               .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
               .LessThanOrEqualTo(50).WithMessage($"{LESS_THAN} 50");

            RuleFor(property => property.YAcc)
               .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
               .NotEmpty().WithMessage(NOTEMPTY)
               .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
               .LessThanOrEqualTo(1000).WithMessage($"{LESS_THAN} 1000");
            #endregion

            #region Z rules
            RuleFor(property => property.ZVelLow)
                    .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
                   .NotEmpty().WithMessage(NOTEMPTY)
                   .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
                   .LessThanOrEqualTo(10).WithMessage($"{LESS_THAN} 10")
                   .LessThan(p => p.ZVelHigh).WithMessage($"{LESS_THAN} {VELHIGH}");

            RuleFor(property => property.ZVelHigh)
               .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
               .NotEmpty().WithMessage(NOTEMPTY)
               .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
               .LessThanOrEqualTo(50).WithMessage($"{LESS_THAN} 50")
               .GreaterThan(p => p.ZVelLow).WithMessage($"{GREATER_THAN} {VELLOW}");

            RuleFor(property => property.ZVelService)
              .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
              .NotEmpty().WithMessage(NOTEMPTY)
              .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
              .LessThanOrEqualTo(50).WithMessage($"{LESS_THAN} 50");

            RuleFor(property => property.ZAcc)
               .GreaterThan(0).WithMessage($"{GREATER_THAN} 0")
               .NotEmpty().WithMessage(NOTEMPTY)
               .Must(p => Regex.IsMatch(p.ToString(), @"^[0-9]+")).WithMessage(NOTVALID)
               .LessThanOrEqualTo(1000).WithMessage($"{LESS_THAN} 1000");
            #endregion
        }
    }
}
