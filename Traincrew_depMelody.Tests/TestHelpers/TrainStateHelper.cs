using System.IO;
using System.Reflection;
using Traincrew_depMelody.Models;

namespace Traincrew_depMelody.Tests.TestHelpers;

public static class TrainStateHelper
{
    /// <summary>
    /// テスト用のAppTrainStateを作成（推奨）
    /// </summary>
    public static AppTrainState CreateAppTrainState(string trainClass = "")
    {
        return new AppTrainState
        {
            Class = trainClass,
            AllClose = true,
            NowTime = TimeSpan.Zero,
            CarStates = new List<AppCarState>(),
            StationList = new List<AppStationInfo>(),
            NowStaIndex = 0,
            DiaName = string.Empty,
            NextStaName = string.Empty
        };
    }

    /// <summary>
    /// リフレクションでTrainCrewInput.TrainStateを作成（レガシー、非推奨）
    /// </summary>
    [Obsolete("Use CreateAppTrainState instead")]
    public static object CreateTrainState(string trainClass)
    {
        // TrainCrewInput.dll を動的に読み込んで TrainState を作成
        var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrainCrewInput.dll");
        var assembly = Assembly.LoadFrom(dllPath);
        var trainStateType = assembly.GetType("TrainCrewInput.TrainState");

        if (trainStateType == null)
        {
            throw new InvalidOperationException("TrainState type not found in TrainCrewInput.dll");
        }

        var trainState = Activator.CreateInstance(trainStateType);
        var classProperty = trainStateType.GetProperty("Class");

        if (classProperty != null && trainState != null)
        {
            classProperty.SetValue(trainState, trainClass);
        }

        return trainState!;
    }
}
