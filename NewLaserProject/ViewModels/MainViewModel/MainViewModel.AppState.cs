using Stateless;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.ViewModels
{
	internal partial class MainViewModel
	{
		private StateMachine<AppState, AppTrigger> _appStateMachine;

		private void InitAppState()
		{
			_appStateMachine = new StateMachine<AppState, AppTrigger>(AppState.Default, FiringMode.Queued);

			_appStateMachine.Configure(AppState.Default)
				.OnEntry(() => 
				{
					IsProcessPanelVisible = false;
				})
				.Permit(AppTrigger.StartLearning, AppState.Learning)
				.PermitIf(AppTrigger.StartProcess, AppState.Processing,() => IsFileLoaded)
				.Ignore(AppTrigger.EndLearning)
				.Ignore(AppTrigger.EndProcess);

			_appStateMachine.Configure(AppState.Processing)
				.OnEntryAsync(async () => {
					OnProcess = true;
					HideCentralPanel(false);
					HideLearningPanel(true);
					//HideVideoPanel(false);
					HideRightPanel(true);
					ChangeViews(IsVideOnCenter: false);
					await StartProcess();
				})
				.OnExit(() =>
				{
					//CancelProcess();
					OnProcess = false;
				})
				.Permit(AppTrigger.EndProcess, AppState.Default)
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
				.Permit(AppTrigger.EndLearning, AppState.Default)
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
			Default,
			Processing,
			Learning
		}
		enum AppTrigger
		{
			StartProcess,
			StartLearning,
			EndProcess,
			EndLearning
		}
	}
}
