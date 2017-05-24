using System;
using Newtonsoft.Json;

namespace CameraApp
{
    internal class FaceModel
    {
        public Guid faceId { get; set; }
        public FaceRectangle faceRectangle { get; set; }
        public FaceAttributes faceAttributes { get; set; }
    }
    internal class FaceRectangle
    {
        [JsonProperty("top")]
        public int Top { get; set; }
        [JsonProperty("left")]
        public int Left { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
    }
    internal class FaceAttributes
    {
        [JsonProperty("gender")]
        public string Gender { get; set; }
        [JsonProperty("age")]
        public float Age { get; set; }
        [JsonProperty("smile")]
        public string Smile { get; set; }
        [JsonProperty("glasses")]
        public string Glasses { get; set; }
    }
}