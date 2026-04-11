using System;
using Newtonsoft.Json;

namespace McpUnity.Models
{
    [Serializable]
    public class UpdateGameObjectRequest
    {
        [JsonProperty("instanceId")]
        public int? InstanceId { get; set; }

        [JsonProperty("objectPath")]
        public string ObjectPath { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("layer")]
        public int? Layer { get; set; }

        [JsonProperty("isActiveSelf")]
        public bool? IsActiveSelf { get; set; }

        [JsonProperty("isStatic")]
        public bool? IsStatic { get; set; }
    }
}
