#nullable enable
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    /// <summary>
    ///     Personal text message.
    /// </summary>
    public abstract class SharedBwoinkSystem : EntitySystem
    {
        // System users
        public static NetUserId SystemUserId { get; } = new NetUserId(Guid.Empty);

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BwoinkTextMessage>(OnBwoinkTextMessage);
        }

        protected virtual void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            // Specific side code in target.
        }

        protected void LogBwoink(BwoinkTextMessage message)
        {
        }

        /// <summary>
        ///     Personal text message.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class BwoinkTextMessage : EntityEventArgs
        {
            /// <summary>
            ///     Message send date.
            /// </summary>
            public DateTime SentAt { get; }

            /// <summary>
            ///     Receiver user identifier.
            /// </summary>
            public NetUserId UserId { get; }

            /// <summary>
            ///     Sender user identifier.
            ///     This is ignored from the client.
            ///     It's checked by the client when receiving a message from the server for bwoink noises.
            ///     This could be a boolean "Incoming", but that would require making a second instance.
            /// </summary>
            public NetUserId TrueSender { get; }

            /// <summary>
            ///     Message text.
            /// </summary>
            public string Text { get; }

            /// <summary>
            ///     .ctor
            /// </summary>
            /// <param name="userId">Receiver user identifier</param>
            /// <param name="trueSender">Sender user identifier</param>
            /// <param name="text">Message text</param>
            /// <param name="sentAt">Message send date</param>
            public BwoinkTextMessage(NetUserId userId, NetUserId trueSender, string text, DateTime? sentAt = default)
            {
                SentAt = sentAt ?? DateTime.Now;
                UserId = userId;
                TrueSender = trueSender;
                Text = text;
            }
        }
    }

    /// <summary>
    ///     Sent by the server to notify all clients when the webhook url is sent.
    ///     The webhook url itself is not and should not be sent.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class BwoinkDiscordRelayUpdated : EntityEventArgs
    {
        public bool DiscordRelayEnabled { get; }

        public BwoinkDiscordRelayUpdated(bool enabled)
        {
            DiscordRelayEnabled = enabled;
        }
    }
}
