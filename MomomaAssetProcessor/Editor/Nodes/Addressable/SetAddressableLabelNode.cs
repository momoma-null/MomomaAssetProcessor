
#if ADDRESSABLES
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityObject = UnityEngine.Object;

#nullable enable

namespace MomomaAssets.GraphView.AssetProcessor
{
    [Serializable]
    [CreateElement(typeof(AssetProcessorGUI), "Addressable/Set Label")]
    sealed class SetAddressableLabelNode : INodeProcessor
    {
        SetAddressableLabelNode() { }

        [SerializeField]
        string m_Label = "";
        [SerializeField]
        bool m_Enable = true;

        public void Initialize(IPortDataContainer portDataContainer)
        {
            portDataContainer.AddInputPort<UnityObject>(isMulti: true);
            portDataContainer.AddOutputPort<UnityObject>(isMulti: true);
        }

        public void Process(ProcessingDataContainer container, IPortDataContainer portDataContainer)
        {
            var assetGroup = container.Get(portDataContainer.InputPorts[0], AssetGroup.combineAssetGroup);
            var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            foreach (var assets in assetGroup)
            {
                var guid = AssetDatabase.AssetPathToGUID(assets.AssetPath);
                var entry = aaSettings.FindAssetEntry(guid);
                entry?.SetLabel(m_Label, m_Enable);
            }
            container.Set(portDataContainer.OutputPorts[0], assetGroup);
        }

        public T DoFunction<T>(IFunctionContainer<INodeProcessor, T> function)
        {
            return function.DoFunction(this);
        }
    }
}
#endif
