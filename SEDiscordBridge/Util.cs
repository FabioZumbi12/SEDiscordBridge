using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace SEDicordBridge
{
    public static class Util
    {
        public static IMyPlayer GetPlayerByNameOrId(string nameOrPlayerId)
        {
            if (!long.TryParse(nameOrPlayerId, out long id))
            {
                foreach (var identity in MySession.Static.Players.GetAllIdentities())
                {
                    if (identity.DisplayName == nameOrPlayerId)
                    {
                        id = identity.IdentityId;
                    }
                }
            }

            MyPlayer.PlayerId playerId;
            if (MySession.Static.Players.TryGetPlayerId(id, out playerId))
            {
                if (MySession.Static.Players.TryGetPlayerById(playerId, out MyPlayer player))
                {
                    return player;
                }
            }

            return null;
        }
    }
}
