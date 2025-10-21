using System.IO;
using System.Reflection;

namespace Traincrew_depMelody.Tests.TestHelpers;

public static class TrainStateHelper
{
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
