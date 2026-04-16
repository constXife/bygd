using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public abstract class BaseNPC : MonoBehaviour, Hoverable, Interactable, IAISuppressed
    {
        private const float MumbleIntervalMin = 30f;
        private const float MumbleIntervalMax = 90f;
        private const float MumbleCullDistance = 20f;
        private const float MumbleTTL = 5f;

        private float _nextTalkTime;
        private float _nextMumbleTime;

        protected abstract string DisplayName { get; }
        protected abstract string ObjectName { get; }
        protected abstract string NameLocKey { get; }
        protected abstract string TalkLocKey { get; }
        protected abstract string GetContextualLine();

        void Start()
        {
            _nextMumbleTime = Time.time + Random.Range(MumbleIntervalMin, MumbleIntervalMax);
        }

        void Update()
        {
            if (Time.time < _nextMumbleTime)
                return;

            _nextMumbleTime = Time.time + Random.Range(MumbleIntervalMin, MumbleIntervalMax);
            Mumble();
        }

        private void Mumble()
        {
            if (Chat.instance == null)
                return;

            string line = GetContextualLine();
            string name = Localization.instance.Localize(NameLocKey);
            Chat.instance.SetNpcText(gameObject, Vector3.up * 2f, MumbleCullDistance, MumbleTTL, name, line, false);
        }

        public void Initialize()
        {
            ConfigureAppearance();

            var tameable = GetComponent<Tameable>();
            if (tameable != null)
                Reflect.Tameable_Tame.Invoke(tameable, null);

            var ai = GetComponent<BaseAI>();
            if (ai != null)
                Reflect.BaseAI_StopMoving.Invoke(ai, null);
        }

        private void ConfigureAppearance()
        {
            if (Reflect.Character_m_name != null)
                Reflect.Character_m_name.SetValue(GetComponent<Character>(), DisplayName);

            gameObject.name = ObjectName;
            DisableComponentByType(Reflect.TraderType);
            DisableComponentByType(Reflect.NpcTalkType);
            DisableComponentByType(Reflect.TalkerType);
        }

        private void DisableComponentByType(System.Type componentType)
        {
            if (componentType == null)
                return;

            Component component = GetComponent(componentType);
            if (component == null)
                return;

            if (Reflect.Talker_m_nameOverride != null && componentType == Reflect.TalkerType)
                Reflect.Talker_m_nameOverride.SetValue(component, DisplayName);

            Behaviour behaviour = component as Behaviour;
            if (behaviour != null)
            {
                behaviour.enabled = false;
                return;
            }

            Destroy(component);
        }

        public string GetHoverText()
        {
            string name = Localization.instance.Localize(NameLocKey);
            string action = Localization.instance.Localize(TalkLocKey);
            return $"{name}\n[<color=yellow><b>$KEY_Use</b></color>] {action}";
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize(NameLocKey);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            if (Time.time < _nextTalkTime)
                return true;

            _nextTalkTime = Time.time + 1.5f;
            string line = GetContextualLine();
            string name = Localization.instance.Localize(NameLocKey);
            user?.Message(MessageHud.MessageType.Center, $"{name}:\n{line}");
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}
