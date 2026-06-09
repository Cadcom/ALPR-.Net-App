using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ALPR
{
    public class BBoxAnnotation
    {
        public int ClassId { get; set; }
        public float Cx { get; set; }  // YOLO normalized 0-1
        public float Cy { get; set; }
        public float W { get; set; }
        public float H { get; set; }
    }

    public class AnnotationBlock
    {
        [JsonPropertyName("boxes")]
        public List<float[]> Boxes { get; set; } = new();
    }

    public class ImageEntry
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "image";
        [JsonPropertyName("file")] public string File { get; set; } = "";
        [JsonPropertyName("url")] public string Url { get; set; } = "";
        [JsonPropertyName("width")] public int Width { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
        [JsonPropertyName("split")] public string Split { get; set; } = "train";
        [JsonPropertyName("annotations")] public AnnotationBlock Annotations { get; set; } = new();
    }

    public class DatasetRoot
    {
        [JsonPropertyName("classes")] public List<string> Classes { get; set; } = new();
        [JsonPropertyName("images")] public List<ImageEntry> Images { get; set; } = new();
    }
}