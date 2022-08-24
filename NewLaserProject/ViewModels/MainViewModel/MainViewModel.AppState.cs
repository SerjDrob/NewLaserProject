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
				.OnEntry(() => { })
				.Permit(AppTrigger.StartLearning, AppState.Learning)
				.PermitIf(AppTrigger.StartProcess, AppState.Processing,() => IsFileLoaded)
				.Ignore(AppTrigger.EndLearning)
				.Ignore(AppTrigger.EndProcess);

			_appStateMachine.Configure(AppState.Processing)
				.OnEntryAsync(async () => {
					OnProcess = true;
					await StartProcess();
				})
				.OnExit(() =>
				{
					CancelProcess();
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
					switch (teacher)
					{
						case Teacher.Corners:
							{
							}
							break;
						case Teacher.CameraOffset:
							{
								await TeachCameraOffsetAsync();
							}
							break;
						case Teacher.ScanatorHorizont:
							{
								await TeachScanatorHorizontAsync();
							}
							break;
						case Teacher.OrthXY:
							{
								await TeachOrthXYAsync();
							}
							break;
						case Teacher.CameraScale:
							{

								await TeachCameraScaleAsync();
							}
							break;
						default:
							break;
					}
				})
				.Permit(AppTrigger.EndLearning, AppState.Default)
				.Ignore(AppTrigger.EndProcess)
				.Ignore(AppTrigger.StartProcess)
				.Ignore(AppTrigger.StartLearning);
			
			_appStateMachine.Activate();
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
