using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Codeworx.Battleship.Player
{
    public class BattleshipRequest
    {
        [JsonPropertyName("gameId")]
        public Guid GameId { get; set; }

        [JsonPropertyName("lastShot")]
        public string LastShot { get; set; }

        [JsonPropertyName("numberOfShots")]
        public int? NumerOfShots { get; set; }

        [JsonPropertyName("board")]
        public string Board { get; set; }
    }
}
