// SPDX-FileCopyrightText: 2021 moonheart08 <moonheart08@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Veritius <veritiusgaming@gmail.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Morb <14136326+Morb0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI
{
    public sealed class AdminAnnounceEui : BaseEui
    {
        private readonly AdminAnnounceWindow _window;

        public AdminAnnounceEui()
        {
            _window = new AdminAnnounceWindow();
            _window.OnClose += () => SendMessage(new CloseEuiMessage());
            // RS14-start
            _window.AnnounceButton.OnPressed += _ => 
            {
                var announcement = AdminAnnounceHelpers.NormalizeText(Rope.Collapse(_window.Announcement.TextRope));
                if (string.IsNullOrWhiteSpace(announcement))
                    return;

                var announceType = (AdminAnnounceType) (_window.AnnounceMethod.SelectedMetadata ?? AdminAnnounceType.Station);

                // CorvaxGoob-TTS-Start
                var voice = "None";
                if (_window.VoiceButton.ItemCount > 0)
                    voice = (string) (_window.VoiceButton.GetItemMetadata(_window.VoiceButton.SelectedId) ?? voice);
                // CorvaxGoob-TTS-End

                SendMessage(new AdminAnnounceEuiMsg.DoAnnounce
                {
                    Announcement = announcement,
                    Announcer = AdminAnnounceHelpers.NormalizeText(_window.Announcer.Text),
                    AnnounceType = announceType,
                    Voice = voice, // CorvaxGoob-TTS
                    CloseAfter = !_window.KeepWindowOpen.Pressed,
                    Global = _window.GlobalAnnouncement.Pressed,
                    ColorHex = AdminAnnounceHelpers.GetValidatedColorHex(announceType, _window.GetCurrentHex()),
                    SoundPath = _window.SoundPath.Text,
                    Sender = _window.EnableSender.Pressed ? AdminAnnounceHelpers.NormalizeText(_window.Sender.Text) : string.Empty,
                });
            };
        }

        public override void Opened() => _window.OpenCentered();
        public override void Closed() => _window.Close();
        // RS14-end
    }
}
