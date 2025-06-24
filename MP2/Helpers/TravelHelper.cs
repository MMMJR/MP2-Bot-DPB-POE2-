using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions;

namespace MP2.Helpers
{
    public static class TravelHelper
    {
        public static async Task<bool> TravelToZone(DatWorldAreaWrapper datWorldAreaWrapper)
        {
            var ret = false;
            
                if (LokiPoe.InstanceInfo.AvailableWaypoints.Any(x => x.Value.Id == datWorldAreaWrapper.Id ))
                {
                    //MP2.Log.DebugFormat("Found Waypoint for {0}", datWorldAreaWrapper.Name);

                    await PlayerAction.TakeWaypoint(datWorldAreaWrapper);
                    ret = true;
                    
                }
                if (datWorldAreaWrapper.Connections.Any(x => x.Id == LokiPoe.CurrentWorldArea.Id))
                {
                    var zone =
                        LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>()
                            .FirstOrDefault(x => x.Name == datWorldAreaWrapper.Name);
                    if (zone != null)
                    {
                        //MP2.Log.DebugFormat("Found Areatransation for {0}", datWorldAreaWrapper.Name);
                        await Move.AtOnce(zone.Position, "Move to area transition");
                        await Coroutines.ReactionWait();
                    var result = await PlayerAction.TakeTransition(zone);
                    if (result)
                        {
                            ret = true;
                            
                        }
                        await Coroutines.ReactionWait();
                    }                    
                }
                                   
            return ret;
        }
    }
}
