using System;
using System.Threading;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineControlsLibrary.CommonDialog;
using NewLaserProject.Classes;
using NewLaserProject.ViewModels.DialogVM;
using Stateless;


namespace NewLaserProject.ViewModels
{
    public partial class MainViewModel
    {
        private StateMachine<AppState, AppTrigger> _appStateMachine;
        private CancellationTokenSource _individualProcCancellationTokenSource;

        public bool IsMainTabOpen { get; set; } = true;
        public bool IsProcessing { get; set; } = false;

        private void InitAppState()
        {
            _appStateMachine = new StateMachine<AppState, AppTrigger>(AppState.Ready, FiringMode.Queued);

            _appStateMachine.Configure(AppState.Ready)
                .OnEntry(() =>
                {
                    IsProcessPanelVisible = false;
                })
                .Permit(AppTrigger.StartLearning, AppState.Learning)
                .PermitIf(AppTrigger.StartProcess, AppState.Processing, () => IsFileLoaded)
                .Permit(AppTrigger.HealthProblem, AppState.NotReady)
                .Ignore(AppTrigger.EndLearning)
                .Ignore(AppTrigger.EndProcess);

            _appStateMachine.Configure(AppState.NotReady)
                .OnEntry(() => { })
                .Permit(AppTrigger.HealthOK, AppState.Ready)
                .Ignore(AppTrigger.StartLearning)
                .Ignore(AppTrigger.StartProcess)
                .Ignore(AppTrigger.EndLearning)
                .Ignore(AppTrigger.EndProcess);

            _appStateMachine.Configure(AppState.Processing)
                .OnEntryAsync(async () =>
                {
                    OnProcess = true;
                    HideCentralPanel(false);
                    HideLearningPanel(true);
                    HideRightPanel(true);
                    ChangeViews(IsVideOnCenter: false);
                    await StartProcessAsync();
                })
                .OnExit(DenyDownloadedProcess)
                .Permit(AppTrigger.EndProcess, AppState.Ready)
                .Ignore(AppTrigger.EndLearning)
                .Ignore(AppTrigger.StartLearning)
                .Ignore(AppTrigger.StartProcess);

            _appStateMachine.Configure(AppState.Learning)
                .OnEntryAsync(async tr =>
                {
                    var teacher = (Teacher)tr.Parameters[0];
                    HideCentralPanel(false);
                    HideLearningPanel(false);
                    HideProcessPanel(true);
                    HideRightPanel(false);
                    ChangeViews(IsVideOnCenter: true);

                    switch (teacher)
                    {
                        case Teacher.Corners:
                            {
                            }
                            break;
                        case Teacher.CameraOffset:
                            {
                                _currentTeacher = await TeachCameraOffsetAsync();
                                /*
								 VideoScreen on the center
								right panel is stepping diagram
								 */
                            }
                            break;
                        case Teacher.ScanatorHorizont:
                            {
                                _currentTeacher = await TeachScanatorHorizontAsync();
                            }
                            break;
                        case Teacher.OrthXY:
                            {
                                _currentTeacher = await TeachOrthXYAsync();
                            }
                            break;
                        case Teacher.CameraScale:
                            {
                                _currentTeacher = await TeachCameraScaleAsync();
                            }
                            break;
                        case Teacher.CameraGroupOffset:
                            {
                                var result = await Dialog.Show<CheckParamsDialog>()
                                                           .SetDialogTitle("Обучение смещения")
                                                           .SetDataContext<AskThicknessVM>(vm => vm.Thickness = WaferThickness)
                                                           .GetCommonResultAsync<double>();
                                if (result?.Success ?? false)
                                {
                                    _currentTeacher = new CameraGroupOffsetTeacher(_coorSystem.ExtractSubSystem(MachineClassLibrary.Classes.LMPlace.FileOnWaferUnderLaser),_laserMachine,_settingsManager, result.CommonResult);
                                }
                            }
                            break;
                        default:
                            {
                                //_currentTeacher = null;
                            }
                            break;
                    }
                    _currentTeacher.TeachingCompleted += _currentTeacher_TeachingCompleted;
                    await _currentTeacher.StartTeach();
                    _canTeach = true;
                })
                .OnExit(() =>
                {
                    HideCentralPanel(false);
                    HideLearningPanel(true);
                    HideProcessPanel(true);
                    HideRightPanel(false);
                    ChangeViews();
                })
                .Permit(AppTrigger.EndLearning, AppState.Ready)
                .Ignore(AppTrigger.EndProcess)
                .Ignore(AppTrigger.StartProcess)
                .Ignore(AppTrigger.StartLearning);

            _appStateMachine.Activate();
        }

        private void _currentTeacher_TeachingCompleted(object? sender, EventArgs e)
        {
            _appStateMachine.Fire(AppTrigger.EndLearning);
        }

        enum AppState
        {
            Ready,
            NotReady,
            Processing,
            Learning
        }
        enum AppTrigger
        {
            StartProcess,
            StartLearning,
            EndProcess,
            EndLearning,
            HealthProblem,
            HealthOK
        }
    }
}
