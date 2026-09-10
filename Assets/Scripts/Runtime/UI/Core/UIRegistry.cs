using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitchTrial.UI
{
    [CreateAssetMenu(fileName = "UIRegistry", menuName = "Witch Trial/UI/UI Registry")]
    public sealed class UIRegistry : ScriptableObject
    {
        [SerializeField] private List<UIPanel> panelPrefabs = new List<UIPanel>();

        public bool TryGetPrefab(Type panelType, out UIPanel prefab)
        {
            for (var i = 0; i < panelPrefabs.Count; i++)
            {
                var candidate = panelPrefabs[i];
                if (candidate != null && candidate.GetType() == panelType)
                {
                    prefab = candidate;
                    return true;
                }
            }

            prefab = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var registeredTypes = new HashSet<Type>();
            for (var i = 0; i < panelPrefabs.Count; i++)
            {
                var prefab = panelPrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                if (!registeredTypes.Add(prefab.GetType()))
                {
                    Debug.LogError(
                        $"UIRegistry contains more than one prefab for {prefab.GetType().Name}.", this);
                }
            }
        }
#endif
    }
}
