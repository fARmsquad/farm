using FarmSimVR.MonoBehaviours.Cinematics;
using TMPro;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.UI
{
    /// <summary>
    /// Sets a world-space name label from the nearest <see cref="NPCController"/> in parents.
    /// </summary>
    public sealed class NpcNameplateBinder : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _roleText;

        internal void Configure(TMP_Text nameText, TMP_Text roleText)
        {
            _nameText = nameText;
            _roleText = roleText;
        }

        private void Start()
        {
            if (_nameText == null)
                _nameText = GetComponentInChildren<TMP_Text>(true);

            var npc = GetComponentInParent<NPCController>();
            if (npc == null || _nameText == null)
                return;

            _nameText.text = npc.NpcName;

            if (_roleText == null)
                return;

            string role = npc.NpcRole;
            bool hasRole = !string.IsNullOrWhiteSpace(role);
            _roleText.gameObject.SetActive(hasRole);
            if (hasRole)
                _roleText.text = role;
        }
    }
}
