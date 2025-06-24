using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game.GameData;

namespace MP2.EXtensions.Mapper
{
    [SuppressMessage("ReSharper", "UnassignedReadonlyField")]
    public static class MapNames
    {
        //[AreaId("MapWorldsLookout")]
        //public static readonly string Lookout;

        static MapNames()
        {
            var areaDict = Dat.WorldAreaDat.RecordsByStrId;
            bool error = false;

            foreach (var field in typeof(MapNames).GetFields())
            {
                var attr = field.GetCustomAttribute<AreaId>();

                if (attr == null)
                    continue;

                if (areaDict.TryGetValue(attr.Id, out var name))
                {
                    field.SetValue(null, name.Name);
                }
                else
                {
                    GlobalLog.Error($"[MapNames] Cannot initialize \"{field.Name}\" field. DatWorldAreas does not contain area with \"{attr.Id}\" id.");
                    error = true;
                }
            }
            //if (error) BotManager.Stop();
        }
    }
}
