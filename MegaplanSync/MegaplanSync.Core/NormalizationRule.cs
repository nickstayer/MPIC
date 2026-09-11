using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MegaplanSync.Core
{
    public class NormalizationRule
    {
        public string ClassPropertyName { get; set; }
        public string ClassPropertyType { get; set; }
        public JsonElement NullEquivalent { get; set; }
    }
}
