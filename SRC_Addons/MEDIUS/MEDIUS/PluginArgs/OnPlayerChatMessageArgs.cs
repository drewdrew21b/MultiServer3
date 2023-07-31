﻿using PSMultiServer.SRC_Addons.MEDIUS.RT.Models.Misc;
using PSMultiServer.SRC_Addons.MEDIUS.MEDIUS.Medius.Models;

namespace PSMultiServer.SRC_Addons.MEDIUS.MEDIUS.PluginArgs
{
    public class OnPlayerChatMessageArgs
    {
        /// <summary>
        /// Source player.
        /// </summary>
        public ClientObject Player { get; set; }

        public Channel Channel { get; set; }

        /// <summary>
        /// Message.
        /// </summary>
        public IMediusChatMessage Message { get; set; }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Player: {Player} " +
                $"Message: {Message}";
        }

    }
}