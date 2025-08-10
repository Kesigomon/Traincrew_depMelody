using System.Globalization;
using System.IO;
using CsvHelper;

namespace Traincrew_depMelody.Repository;

public interface ITrackRepository
{
    /// <summary>
    /// 軌道回路を受け取り、駅名と番線のペアを返す。ホームトラック上でない場合はnullを返す。
    /// </summary>
    /// <param name="trackCircuits"></param>
    /// <returns></returns>
    (string, int)? GetTrackByTrackCircuits(HashSet<string> trackCircuits);
}

public class TrackRepository: ITrackRepository
{
    private readonly Dictionary<HashSet<string>, (string StationName, int TrackNumber)> _trackCircuitToTrack = new(HashSet<string>.CreateSetComparer());

    public TrackRepository()
    {
        LoadTracksFromCsv();
    }

    private void LoadTracksFromCsv()
    {
        var filePath = Environment.CurrentDirectory + @"\Csv\Track.csv";
        using var reader = new StringReader(File.ReadAllText(filePath));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        csv.Read();
        csv.ReadHeader();
        
        while (csv.Read())
        {
            var stationName = csv.GetField<string>("駅名")!;
            var trackNumber = csv.GetField<int>("番線");
            var trackCircuit1 = csv.GetField<string>("対応軌道回路1");
            var trackCircuit2 = csv.GetField<string>("対応軌道回路2");
            var trackCircuit3 = csv.GetField<string>("対応軌道回路3");
            
            var trackInfo = (stationName, trackNumber);
            var key = new HashSet<string>();  
            // 軌道回路1は必須
            if (!string.IsNullOrEmpty(trackCircuit1))
            {
                key.Add(trackCircuit1);
            }
            
            // 軌道回路2があれば追加
            if (!string.IsNullOrEmpty(trackCircuit2) && trackCircuit2 != "なし")
            {
                key.Add(trackCircuit2);
            }
            
            // 軌道回路3があれば追加
            if (!string.IsNullOrEmpty(trackCircuit3) && trackCircuit3 != "なし")
            {
                key.Add(trackCircuit3);
            }
            _trackCircuitToTrack.Add(key, trackInfo);
        }
    }

    public (string, int)? GetTrackByTrackCircuits(HashSet<string> trackCircuitSet)
    {
        return _trackCircuitToTrack.TryGetValue(trackCircuitSet, out (string, int) result) ? result : null;
    }
}