using System.Linq;
using UnityEngine.UIElements;

namespace Unity.Services.Multiplayer.Editor
{
    class SessionsViewer
    {
        internal ISession Session;
        internal UIController UI;

        UIElement SessionContainer;

        public SessionsViewer(UIController ui, ISession session)
        {
            UI = ui;
            Session = session;
        }

        public void CreateGUI()
        {
            SessionContainer = UI.Element().SetMargin(5, 0, 5, 0);

            RefreshSessionGUI();

            UI.Button("Refresh", RefreshSessionGUI)
                .SetWidth(150);
        }

        private void RefreshSessionGUI()
        {
            if (SessionContainer == null || Session == null)
                return;

            SessionContainer.Clear();

            using (SessionContainer.Scope())
            {
                CreateSessionGUI();
            }
        }

        void CreateSessionGUI()
        {
            if (!string.IsNullOrEmpty(Session.Type))
            {
                UI.H4(Session.Type.ToString());
            }

            CreateSessionInfoGUI();
            CreatePropertiesGUI();
            CreatePlayersGUI();
        }

        void CreateSessionInfoGUI()
        {
            using (UI.VerticalElement().Scope())
            {
                UI.Label("Session Info").SetFontStyleBold().SetMarginTop(5);
                UI.Separator();

                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("Session State").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.State.ToString());
                }
                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("Session Id").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.Id);
                }

                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("Name").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.Name);
                }

                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("MaxPlayers").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.MaxPlayers.ToString());
                }

                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("IsPrivate").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.IsPrivate.ToString());
                }

                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("IsLocked").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.IsLocked.ToString());
                }

                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("Code").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.Code);
                }

                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("Host").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.Host);
                }

                using (UI.HorizontalElement().Scope())
                {
                    UI.Label("Is Host").SetWidth(100);
                    UI.SelectableLabel().SetText(Session.IsHost.ToString());
                }
            }
        }

        void CreatePlayersGUI()
        {
            UI.Label("Players").SetFontStyleBold().SetMarginTop(5);
            UI.Separator();

            for (var i = 0; i != Session.Players.Count; ++i)
            {
                CreatePlayerGUI(Session.Players[i]);
            }
        }

        void CreatePlayerGUI(IReadOnlyPlayer player)
        {
            using (UI.HeaderPanel().Scope())
            {
                UI.SelectableLabel().SetMargin(0).SetPadding(0).SetFontStyleBold().SetText(player.Id.ToString());

                if (player == Session.CurrentPlayer)
                {
                    UI.Label("*");
                }
            }

            using (UI.ContentPanel(true).SetMarginBottom(5).Scope())
            {
                using (UI.HorizontalLayout().Scope())
                {
                    UI.Label("Joined:").SetWidth(100);
                    UI.SelectableLabel().SetText(player.Joined.ToLocalTime().ToShortTimeString()).SetWidth(100);
                }

                using (UI.HorizontalLayout().Scope())
                {
                    UI.Label("Last Updated:").SetWidth(100);
                    UI.SelectableLabel().SetText(player.LastUpdated.ToLocalTime().ToShortTimeString()).SetWidth(100);
                }

                UI.Label("Player Properties").SetMarginTop(5).SetFontStyleBold();

                if (player.Properties.Count == 0)
                {
                    using (UI.HorizontalLayout().Scope())
                    {
                        UI.Label("None.");
                    }
                }
                else
                {
                    foreach (var property in player.Properties)
                    {
                        using (UI.HorizontalLayout().Scope())
                        {
                            UI.Label(property.Key).SetWidth(150);
                            UI.Label().SetText(property.Value.Visibility.ToString()).SetWidth(100);
                            UI.Label().SetText(property.Value.Value).Wrap().SetFlexShrink(1);
                        }
                    }
                }
            }
        }

        void CreatePropertiesGUI()
        {
            UI.Label("Session Properties").SetFontStyleBold().SetMarginTop(5);
            UI.Separator();

            if (Session.Properties.Count == 0)
            {
                using (UI.HorizontalLayout().Scope())
                {
                    UI.Label("None.");
                }
            }
            else
            {
                for (var i = 0; i != Session.Properties.Count; ++i)
                {
                    var property = Session.Properties.ElementAt(i);

                    using (UI.HorizontalLayout().Scope())
                    {
                        UI.Label().SetText(property.Key).SetWidth(150);
                        UI.Label().SetText(property.Value.Visibility.ToString()).SetWidth(100);
                        UI.Label().SetText(property.Value.Value).Wrap().SetFlexShrink(1);
                    }
                }
            }
        }
    }
}
