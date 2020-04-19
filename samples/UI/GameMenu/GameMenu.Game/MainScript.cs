// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace GameMenu
{
    /// <summary>
    /// Script controller for the main scene.
    /// </summary>
    public class MainScript : UISceneBase
    {
        /// <summary>
        /// Default player name.
        /// </summary>
        private const string DefaultName = "John Doe";
        private const int MaximumStar = 3;

        private static readonly List<string> ShipNameList = new List<string>
        {
            "red_ship", "green_ship", "blue_ship", "blue_ship", "yellow_ship", "yellow_ship", "cyan_ship"
        };


        private readonly List<SpaceShip> shipList = new List<SpaceShip>();
        private int money;
        private int bonus;
        private int lifeStatus;
        private int powerStatus;
        private int controlStatus;
        private int speedStatus;
        
        private int activeShipIndex; // Current SpaceShip of the character

        private readonly List<int> starSpriteIndices = new List<int>();
        private readonly List<int> borderStarSpriteIndices = new List<int>();

        #region Visuals

        private UIPage page;

        private ModalElement shipSelectPopup; // Root of SpaceShip select popup
        private ModalElement welcomePopup; // Root of welcome popup

        // Life gauge
        private RectangleF gaugeBarRegion;
        private Grid lifebarGrid;
        private Sprite lifebarGaugeImage;
        // Counters
        private TextBlock bonusCounter;
        private TextBlock lifeCounter;
        private TextBlock moneyCounter;
        // Name of the character
        private TextBlock nameTextBlock;
        private ImageElement currentShipImage;
        // Status stars
        private ImageElement controlStatusStar;
        private ImageElement powerStatusStar;
        private ImageElement speedStatusStar;

        #endregion // Visuals

        /// <summary>
        /// Spritesheet containing the sprites of the main scene.
        /// </summary>
        public SpriteSheet MainSceneImages { get; set; }

        /// <summary>
        /// UI library containing the modal popups and the ship button template.
        /// </summary>
        public UILibrary UILibrary { get; set; }

        private int Bonus
        {
            set
            {
                bonus = value;
                bonusCounter.Text = CreateBonusCountText();
            }
            get { return bonus; }
        }

        private int ControlStatus
        {
            get { return controlStatus; }
            set
            {
                if (value > MaximumStar) return;
                controlStatus = value;
                ((SpriteFromSheet)controlStatusStar.Source).CurrentFrame = starSpriteIndices[controlStatus];
                shipList[activeShipIndex].Control = controlStatus;
            }
        }

        private int LifeStatus
        {
            get { return lifeStatus; }
            set
            {
                lifeStatus = value;
                lifeCounter.Text = CreateLifeCountText();
            }
        }

        private int PowerStatus
        {
            get { return powerStatus; }
            set
            {
                if (value > MaximumStar) return;
                powerStatus = value;
                ((SpriteFromSheet)powerStatusStar.Source).CurrentFrame = starSpriteIndices[powerStatus];
                shipList[activeShipIndex].Power = powerStatus;
            }
        }

        private int SpeedStatus
        {
            get { return speedStatus; }
            set
            {
                if (value > MaximumStar) return;
                speedStatus = value;
                ((SpriteFromSheet)speedStatusStar.Source).CurrentFrame = starSpriteIndices[speedStatus];
                shipList[activeShipIndex].Speed = speedStatus;
            }
        }

        private int Money
        {
            get { return money; }
            set
            {
                money = value;
                moneyCounter.Text = CreateMoneyCountText();
            }
        }

        public override void Start()
        {
            base.Start();
            ShowWelcomePopup();
        }

        protected override void LoadScene()
        {
            // Preload stars
            starSpriteIndices.Add(MainSceneImages.FindImageIndex("star0"));
            starSpriteIndices.Add(MainSceneImages.FindImageIndex("star1"));
            starSpriteIndices.Add(MainSceneImages.FindImageIndex("star2"));
            starSpriteIndices.Add(MainSceneImages.FindImageIndex("star3"));
            borderStarSpriteIndices.Add(MainSceneImages.FindImageIndex("bstar0"));
            borderStarSpriteIndices.Add(MainSceneImages.FindImageIndex("bstar1"));
            borderStarSpriteIndices.Add(MainSceneImages.FindImageIndex("bstar2"));
            borderStarSpriteIndices.Add(MainSceneImages.FindImageIndex("bstar3"));

            // Create space ships
            var random = new Random();
            for (var i = 0; i < ShipNameList.Count; i++)
            {
                shipList.Add(new SpaceShip
                {
                    Name = ShipNameList[i],
                    Power = random.Next(MaximumStar + 1),
                    Control = random.Next(MaximumStar + 1),
                    Speed = random.Next(MaximumStar + 1),
                    IsLocked = (i % 3) == 2,
                });
            }

            // Initialize UI
            page = Entity.Get<UIComponent>().Page;
            InitializeMainPage();
            InitializeShipSelectionPopup();
            InitializeWelcomePopup();

            // Add pop-ups to the overlay
            var overlay = (UniformGrid) page.RootElement;
            overlay.Children.Add(shipSelectPopup);
            overlay.Children.Add(welcomePopup);

            Script.AddTask(FillLifeBar);
        }

        private async Task FillLifeBar()
        {
            var gaugePercentage = 0.15f;

            while (gaugePercentage < 1f)
            {
                await Script.NextFrame();

                gaugePercentage = Math.Min(1f, gaugePercentage + (float)Game.UpdateTime.Elapsed.TotalSeconds * 0.02f);

                var gaugeCurrentRegion = lifebarGaugeImage.Region;
                gaugeCurrentRegion.Width = gaugePercentage * gaugeBarRegion.Width;
                lifebarGaugeImage.Region = gaugeCurrentRegion;

                lifebarGrid.ColumnDefinitions[1].SizeValue = gaugeCurrentRegion.Width / gaugeBarRegion.Width;
                lifebarGrid.ColumnDefinitions[2].SizeValue = 1 - lifebarGrid.ColumnDefinitions[1].SizeValue;
            }
        }

        private bool CanPurchase(int requireMoney, int requireBonus)
        {
            return Money >= requireMoney && Bonus >= requireBonus;
        }

        private void CloseShipSelectPopup()
        {
            shipSelectPopup.Visibility = Visibility.Collapsed;
        }

        private void CloseWelcomePopup()
        {
            welcomePopup.Visibility = Visibility.Collapsed;
        }

        private string CreateBonusCountText()
        {
            return bonus.ToString("D3");
        }

        private string CreateLifeCountText()
        {
            return "x" + lifeStatus;
        }

        private string CreateMoneyCountText()
        {
            return money.ToString("D3");
        }

        private UIElement CreateShipSelectionItem(SpaceShip spaceShip)
        {
            var shipPanel = UILibrary.InstantiateElement<Panel>("ShipButton");
            var shipButton = shipPanel.FindVisualChildOfType<ButtonBase>("shipButton");
            var shipImage = shipButton.FindVisualChildOfType<ImageElement>("shipImage");

            // Update spaceship
            spaceShip.PowerImageElement = shipButton.FindVisualChildOfType<ImageElement>("powerImage");
            spaceShip.ControlImageElement = shipButton.FindVisualChildOfType<ImageElement>("controlImage");
            spaceShip.SpeedImageElement = shipButton.FindVisualChildOfType<ImageElement>("speedImage");

            var shipIndex = MainSceneImages.FindImageIndex(spaceShip.Name);
            ((SpriteFromSheet) shipImage.Source).CurrentFrame = shipIndex;

            shipButton.Click += delegate
            {
                activeShipIndex = shipList.FindIndex(w => w.Name == spaceShip.Name);
                ((SpriteFromSheet)currentShipImage.Source).CurrentFrame = shipIndex;

                PowerStatus = spaceShip.Power;
                ControlStatus = spaceShip.Control;
                SpeedStatus = spaceShip.Speed;

                CloseShipSelectPopup();
            };
            shipButton.IsEnabled = !spaceShip.IsLocked;

            if (spaceShip.IsLocked)
            {
                var lockIconElement = shipPanel.FindVisualChildOfType<ImageElement>("lockIcon");
                lockIconElement.Visibility = Visibility.Visible;
            }

            return shipPanel;
        }

        private void InitializeMainPage()
        {
            var rootElement = page.RootElement;

            // counters
            bonusCounter = rootElement.FindVisualChildOfType<TextBlock>("bonusCounter");
            lifeCounter = rootElement.FindVisualChildOfType<TextBlock>("lifeCounter");
            moneyCounter = rootElement.FindVisualChildOfType<TextBlock>("moneyCounter");
            Bonus = 30;
            LifeStatus = 3;
            Money = 30;

            // lifebar
            lifebarGaugeImage = MainSceneImages["life_bar"];
            lifebarGrid = rootElement.FindVisualChildOfType<Grid>("lifebarGrid");
            gaugeBarRegion = lifebarGaugeImage.Region;

            // character name
            nameTextBlock = rootElement.FindVisualChildOfType<TextBlock>("nameTextBlock");

            // explanation
            // FIXME: UI asset should support multiline text
            var explanationText = rootElement.FindVisualChildOfType<TextBlock>("explanationText");
            explanationText.Text = "Pictogram-based alphabets are easily supported.\n日本語も簡単に入れることが出来ます。";

            // status stars
            var statusPanel = rootElement.FindVisualChildOfType<UniformGrid>("statusPanel");
            powerStatusStar = statusPanel.FindVisualChildOfType<ImageElement>("powerStatusStar");
            controlStatusStar = statusPanel.FindVisualChildOfType<ImageElement>("controlStatusStar");
            speedStatusStar = statusPanel.FindVisualChildOfType<ImageElement>("speedStatusStar");
            PowerStatus = shipList[activeShipIndex].Power;
            ControlStatus = shipList[activeShipIndex].Control;
            SpeedStatus = shipList[activeShipIndex].Speed;

            // ship selection
            var currentShipButton = rootElement.FindVisualChildOfType<Button>("currentShipButton");
            currentShipButton.Click += delegate
            {
                // Once click, update the SpaceShip status pop-up and show it.
                UpdateShipStatus();
                ShowShipSelectionPopup();
            };
            currentShipImage = currentShipButton.FindVisualChildOfType<ImageElement>("currentShipImage");

            // upgrade buttons
            var statusUpgradePanel = rootElement.FindVisualChildOfType<UniformGrid>("statusUpgradePanel");
            SetupStatusButton(statusUpgradePanel.FindVisualChildOfType<ButtonBase>("powerStatusButton"), 2, 0, () => PowerStatus, () => PowerStatus++);
            SetupStatusButton(statusUpgradePanel.FindVisualChildOfType<ButtonBase>("controlStatusButton"), 2, 0, () => ControlStatus, () => ControlStatus++);
            SetupStatusButton(statusUpgradePanel.FindVisualChildOfType<ButtonBase>("speedStatusButton"), 2, 0, () => SpeedStatus, () => SpeedStatus++);
            SetupStatusButton(statusUpgradePanel.FindVisualChildOfType<ButtonBase>("lifeStatusButton"), 1, 1, () => 0, () => LifeStatus++);

            // quit button
            var quitButton = rootElement.FindVisualChildOfType<Button>("quitButton");
            quitButton.Click += delegate { UIGame.Exit(); };
        }

        private void InitializeShipSelectionPopup()
        {
            shipSelectPopup = UILibrary.InstantiateElement<ModalElement>("ShipSelectPopup");
            shipSelectPopup.SetPanelZIndex(1);

            // Layout elements in vertical StackPanel
            var contentStackpanel = shipSelectPopup.FindVisualChildOfType<StackPanel>("contentStackPanel");

            // Create and Add SpaceShip to the stack layout
            foreach (var ship in shipList)
                contentStackpanel.Children.Add(CreateShipSelectionItem(ship));

            // Uncomment those lines to have an example of stack panel item virtualization
            //var shipInitialCount = shipList.Count;
            //contentStackpanel.ItemVirtualizationEnabled = true;
            //for (var i = 0; i < 200; i++)
            //{
            //    shipList.Add(new SpaceShip { Name = shipList[i % shipInitialCount].Name });
            //    contentStackpanel.Children.Add(CreateShipSelectionItem(shipList[shipList.Count - 1]));
            //}

            UpdateShipStatus();
            CloseShipSelectPopup();
        }

        private void InitializeWelcomePopup()
        {
            welcomePopup = UILibrary.InstantiateElement<ModalElement>("WelcomePopup");
            welcomePopup.SetPanelZIndex(1);

            // FIXME: UI asset should support multiline text
            var welcomeText = welcomePopup.FindVisualChildOfType<TextBlock>("welcomeText");
            welcomeText.Text = "Welcome to stride UI sample.\nPlease name your character";
            
            var cancelButton = welcomePopup.FindVisualChildOfType<Button>("cancelButton");
            cancelButton.Click += delegate
            {
                nameTextBlock.Text = DefaultName;
                CloseWelcomePopup();
            };

            var nameEditText = welcomePopup.FindVisualChildOfType<EditText>("nameEditText");
            nameEditText.Text = DefaultName;
            var validateButton = welcomePopup.FindVisualChildOfType<Button>("validateButton");
            validateButton.Click += delegate
            {
                nameTextBlock.Text = nameEditText.Text.Trim();
                CloseWelcomePopup();
            };
        }

        private void PurchaseWithMoney(int requireMoney)
        {
            Money -= requireMoney;
        }

        private void PurchaseWithBonus(int requireBonus)
        {
            Bonus -= requireBonus;
        }

        private void SetupStatusButton(ButtonBase button, int moneyCost, int bonuscost, Func<int> getProperty, Action setProperty)
        {
            button.Click += delegate
            {
                if (!CanPurchase(moneyCost, bonuscost) || getProperty() >= MaximumStar)
                    return;

                setProperty();
                PurchaseWithBonus(bonuscost);
                PurchaseWithMoney(moneyCost);
            };
        }

        private void ShowShipSelectionPopup()
        {
            shipSelectPopup.Visibility = Visibility.Visible;
        }

        public void ShowWelcomePopup()
        {
            welcomePopup.Visibility = Visibility.Visible;
        }

        private void UpdateShipStatus()
        {
            foreach (var ship in shipList)
            {
                ((SpriteFromSheet)ship.PowerImageElement.Source).CurrentFrame = borderStarSpriteIndices[ship.Power];
                ((SpriteFromSheet)ship.ControlImageElement.Source).CurrentFrame = borderStarSpriteIndices[ship.Control];
                ((SpriteFromSheet)ship.SpeedImageElement.Source).CurrentFrame = borderStarSpriteIndices[ship.Speed];
            }
        }

        private class SpaceShip
        {
            public string Name;
            public int Power;
            public int Control;
            public int Speed;
            public bool IsLocked;
            public ImageElement PowerImageElement;
            public ImageElement ControlImageElement;
            public ImageElement SpeedImageElement;
        }
    }
}
