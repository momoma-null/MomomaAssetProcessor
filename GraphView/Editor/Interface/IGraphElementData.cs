using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

#nullable enable

namespace MomomaAssets.GraphView
{
    interface IGraphElementData : IFunctionProxy<IGraphElementData>
    {
        string GraphElementName { get; }
        int Priority { get; }
        GraphElement Deserialize();
        void SetPosition(GraphElement graphElement, Rect position);
        void DeserializeOverwrite(GraphElement graphElement, UnityEditor.Experimental.GraphView.GraphView graphView);
        void ReplaceGuid(Dictionary<string, string> guids);
    }
}
