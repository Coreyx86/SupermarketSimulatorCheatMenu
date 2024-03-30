using UnityEngine;
using MelonLoader;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Collections;
using HarmonyLib;
using System.Management.Instrumentation;
using System.IO;
using DG.Tweening;
using Lean.Pool;
using MyBox;
using System.Reflection.Emit;

namespace SupermarketSimulatorCheatMenu
{
    public class Mod : MelonMod
    {
        #region Game Objects

        CheckoutManager m_checkOutManager;
        InventoryManager m_inventoryManager;
        DeliveryManager m_deliveryManager;
        DisplayManager m_displayManager;
        LocalizationManager m_localizationManager;

        #endregion

        #region Field Declarations

        private const string c_failedToFindNecessaryGameObjects = "Failed to find necessary game objects.";
        static string s_addProductHookLoggerFileName = "AddProductHookLog.txt";
        private bool m_showMenu;
        private CursorLockMode m_originalCursorLockMode = Cursor.lockState;
        private string m_currentScene = "";
        private Rect m_windowRect = new Rect(20, 20, 300, 500);
        #endregion

        #region Mod Declarations

        private List<IModOption> m_options = new List<IModOption>();
        private ButtonOption m_deliverDisplayedProducts;
        private ButtonOption m_refillDisplays;
        private ToggleOption m_fasterCashiers;
        private ButtonOption m_adjustPrices;

        #endregion

        #region Mod Events
        public override void OnUpdate()
        {
            if (m_currentScene=="Main Scene")
            {
                if (GameObject.Find("Delivery Manager").GetComponent<DeliveryManager>() is DeliveryManager
                    deliveryMgr)
                {
                    m_deliveryManager = deliveryMgr;
                }

                if (GameObject.Find("Inventory Manager")?.GetComponent<InventoryManager>() is InventoryManager invMgr)
                {
                    m_inventoryManager = invMgr;
                }

                if (Input.GetKeyUp(KeyCode.J))
                {
                    m_showMenu = !m_showMenu;
                    Cursor.visible = m_showMenu;
                    Cursor.lockState = m_showMenu ? CursorLockMode.None : m_originalCursorLockMode;

                    LoggerInstance.Msg("Attempting to update inventory");
                }
            }
        }

        public override void OnInitializeMelon()
        {
            //Some mod options might be better initialized when the scene is initialized
            m_options.Clear();

            float spaceBetweenOptions = 35;
            float startY = 50;
            float x = 20;

            //Add Deliver Displayed Products
            m_deliverDisplayedProducts = new ButtonOption("",
                "Deliver Displayed Products",
                "Automatically deliver 1 box of each currently displayed product.",
                new Rect(x, startY + (m_options.Count * spaceBetweenOptions), 250, 30),
                false);
            m_deliverDisplayedProducts.OptionInteraction += OnDeliverDisplayedProducts;
            m_options.Add(m_deliverDisplayedProducts);

            //Add Refill Displays
            m_refillDisplays = new ButtonOption("",
                "Refill Displays",
                "Refill every display.",
                new Rect(x, startY + (m_options.Count * spaceBetweenOptions), 250, 30),
                false);
            m_refillDisplays.OptionInteraction += OnRefillDisplays;
            m_options.Add(m_refillDisplays);

            //Add Adjust Prices
            m_adjustPrices = new ButtonOption("",
                "Adjust Product Prices",
                "Automatically sets the price of every displayed product to an optimal price based on the current market price of the product.",
                new Rect(x, startY + (m_options.Count * spaceBetweenOptions), 250, 30),
                false);
            m_adjustPrices.OptionInteraction += OnAdjustProductPrices;
            m_options.Add(m_adjustPrices);

            //Add Faster Cashiers
            m_fasterCashiers = new ToggleOption("",
                "Faster Cashiers",
                "Increases the checkout speed of hired cashiers.",
                new Rect(x, startY + (m_options.Count * spaceBetweenOptions), 250, 30),
                false); //TODO: Setup settings to read/write the mod menu option values
            m_fasterCashiers.OptionInteraction += OnFasterCashiers;
            m_options.Add(m_fasterCashiers);
        }

        private void DrawMenu()
        {
            if (m_showMenu)
            {
                m_windowRect = GUI.Window(0, m_windowRect, OnDrawCheatMenuWindow, "Supermarket Cheat Menu");
            }
        }

        private void OnDrawCheatMenuWindow(int a_windowId)
        {
            foreach (IModOption option in m_options)
            {
                option.DrawOption();
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            m_currentScene = sceneName;
            if(sceneName == "Main Scene")
            {
                MelonEvents.OnGUI.Subscribe(DrawMenu, 100);

                if (File.Exists(s_addProductHookLoggerFileName))
                {
                    File.Delete(s_addProductHookLoggerFileName);
                }

                if (GameObject.Find("Checkout Manager")?.GetComponent<CheckoutManager>() is CheckoutManager checkoutMgr)
                {
                    m_checkOutManager = checkoutMgr;
                }

                if (GameObject.Find("Display Manager")?.GetComponent<DisplayManager>() is DisplayManager displayMgr)
                {
                    m_displayManager = displayMgr;
                }

                if (GameObject.Find("Localization Manager")?.GetComponent<LocalizationManager>() is LocalizationManager localizationMgr)
                {
                    m_localizationManager = localizationMgr;
                }
            }
        }
        #endregion

        #region Option Event Handlers

        /// <summary>
        /// Patches the automated checkouts to go a little bit faster.
        /// </summary>
        private void PatchAutomatedCheckout(bool a_enabled)
        {
            try
            {
                if (Helper.TryGetField<CheckoutManager, List<Checkout>>(m_checkOutManager, "m_Checkouts", out List<Checkout> checkouts))
                {
                    foreach (Checkout checkout in checkouts)
                    {
                        AutomatedCheckout autoCheckout = checkout.gameObject.GetComponent<AutomatedCheckout>();
                        if (autoCheckout == null)
                        {
                            LoggerInstance.Error("AutomatedCheckout was not found on checkout");
                        }

                        if (Helper.TrySetField<AutomatedCheckout, float>(autoCheckout, "m_FinishingPaymentDuration", a_enabled ? 1 : 2)) //Default 2.0
                        {
                            LoggerInstance.Msg("m_FinishingPaymentDuration modified");
                        }

                        //if (Helper.TrySetField<AutomatedCheckout, float>(autoCheckout, "m_IntervalAfterScanningAll", 1)) //Default 1.0
                        //{
                        //    LoggerInstance.Msg("m_FinishingPaymentDuration set to 1.0");
                        //}

                        if (Helper.TrySetField<AutomatedCheckout, float>(autoCheckout, "m_ScanningInterval", a_enabled ? 0.75f : 1.5f)) //Default 1.5
                        {
                            LoggerInstance.Msg("m_FinishingPaymentDuration modified");
                        }

                        LoggerInstance.Msg("Fast cashiers: " + a_enabled);
                    }
                }
                else
                {
                    LoggerInstance.Error("NO CHECKOUTS FOUND ON CHECKOUT MANAGER");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("FAILED GETTING CHECKOUTS: " + ex.Message);
            }
        }

        private void OnRefillDisplays(object obj)
        {
            if (m_displayManager == null)
            {
                LoggerInstance.Error("Failed to find necessary display manager");
            }

            try
            {
                if (!Helper.TryGetField<DisplayManager, List<Display>>(m_displayManager, "m_Displays",
                        out List<Display> displays))
                {
                    LoggerInstance.Error("Failed to get m_Displays from display manager");
                    return;
                }


                LoggerInstance.Msg("Display count " + displays.Count);

                foreach (Display display in displays)
                {
                    if (!Helper.TryGetField<Display, DisplaySlot[]>(display, "m_DisplaySlots",
                            out DisplaySlot[] slots))
                    {
                        LoggerInstance.Error("Failed to get m_DisplaySlots");
                        return;
                    }

                    foreach (DisplaySlot slot in slots)
                    {
                        if ((slot.HasProduct || slot.Data.FirstItemID > 0) && !slot.Full)
                        {
                            LoggerInstance.Msg("Display: " + display.ID + " Prodct: " + GetProductName(slot.ProductID) + " | " + Singleton<PriceManager>.Instance.SellingPrice(slot.ProductID) + " | Full: " + slot.Full);

                            //slot.Data = newItem;
                            IEnumerator addProductsToSlotCR = AddProductsToSlotCR(slot);
                            m_displayManager.StartCoroutine(addProductsToSlotCR);
                        }
                    }
                }

                LoggerInstance.Msg("Attempted to spawn products in all displays");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Error trying to refill displays", ex);
            }
        }

        private void OnDeliverDisplayedProducts(object a_value)
        {
            if (m_inventoryManager == null || m_deliveryManager == null)
            {
                LoggerInstance.Error(c_failedToFindNecessaryGameObjects);
                return;
            }

            try
            {
                LoggerInstance.Msg(m_inventoryManager.DisplayedProducts.Count + " displayed products");
                for (int i = 0; i < m_inventoryManager.DisplayedProducts.Count; i++)
                {
                    int prodId = m_inventoryManager.DisplayedProducts[i];

                    ItemQuantity newItem = new ItemQuantity
                    {
                        Products = new Dictionary<int, int>
                        {
                            {
                                prodId,
                                1
                            }
                        }
                    };

                    MarketShoppingCart.CartData cartData = new MarketShoppingCart.CartData();
                    cartData.ProductInCarts = new List<ItemQuantity>();
                    cartData.ProductInCarts.Add(newItem);

                    m_deliveryManager.Delivery(cartData);
                    LoggerInstance.Msg("OnDeliverDisplayedProducts : Attempting to deliver...");
                    //invMgr.AddProductToDisplay(newItem);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Failed to add item to displayed products: ", ex);
            }
        }

        private void OnFasterCashiers(object obj)
        {
            if (m_checkOutManager == null)
            {
                LoggerInstance.Error(c_failedToFindNecessaryGameObjects);
            }

            if (obj is bool enabled)
            {
                PatchAutomatedCheckout(enabled);
            }
            else
            {
                LoggerInstance.Error("OnFasterCashiers : Expecting boolean value from event handler");
            }

        }

        private void OnAdjustProductPrices(object a_value)
        {
            if (m_displayManager == null)
            {
                LoggerInstance.Error("Failed to find necessary display manager");
            }

            try
            {
                if (!Helper.TryGetField<DisplayManager, List<Display>>(m_displayManager, "m_Displays",
                        out List<Display> displays))
                {
                    LoggerInstance.Error("Failed to get m_Displays from display manager");
                    return;
                }

                foreach (Display display in displays)
                {
                    if (!Helper.TryGetField<Display, DisplaySlot[]>(display, "m_DisplaySlots",
                            out DisplaySlot[] slots))
                    {
                        LoggerInstance.Error("Failed to get m_DisplaySlots");
                        return;
                    }

                    //Loop through all display slots and set the new price
                    foreach (DisplaySlot slot in slots)
                    {
                        if (slot.HasProduct || slot.Data.FirstItemID > 0)
                        {
                            ProductSO productSO = Singleton<IDManager>.Instance.ProductSO(slot.ProductID);
                            if (productSO == null)
                            {
                                LoggerInstance.Error("Failed to get ProductSO for " + slot.ProductID);
                                return;
                            }

                            //Calculate a new price based on the current market price and the max price
                            float currentCost = Singleton<PriceManager>.Instance.CurrentCost(slot.ProductID);
                            float currentMarketPrice = (float)Math.Round((double)(currentCost + currentCost * productSO.OptimumProfitRate / 100f), 2);
                            float maxPrice = (float)Math.Round((double)(currentCost + currentCost * productSO.MaxProfitRate / 100f), 2);

                            float newProfitRate = CalculateOptimalProfitRate(slot.ProductID);
                            float newPrice = (float)Math.Round((double)(currentCost + currentCost * newProfitRate / 100f), 2);

                            float calculatePurchaseChance = CalculatePurchaseChance(slot.ProductID, newProfitRate, out PricingState pricingState);

                            Pricing price = new Pricing(slot.ProductID, newPrice);
                            Singleton<PriceManager>.Instance.PriceSet(price);

                            LoggerInstance.Msg($"{productSO.ProductName} Optimum Profit Rate: {productSO.OptimumProfitRate} Max Profit Rate: {productSO.MaxProfitRate} Current Cost: {currentCost:C} Market Price: {currentMarketPrice:C} Max Price: {maxPrice:C} New Profit Rate {newProfitRate} New Price: {newPrice} New Purchase Chance: {calculatePurchaseChance} Pricing State: {pricingState}");
                            //LoggerInstance.Msg($"Set Price of {productSO.ProductName} to ${newPrice:C}");

                            SetDisplaySlotPrice(slot.ProductID, slot);
                        }
                    }
                }

                LoggerInstance.Msg("Attempted to set product prices");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Error trying to refill displays", ex);
            }
        }
        #endregion

        #region Helpers

        private Product GetProduct(int a_productId, Transform a_transform)
        {
            Product product = null;
            try
            {
                product = LeanPool.Spawn<Product>(Singleton<IDManager>.Instance.ProductSO(a_productId).ProductPrefab, a_transform, false);
                product.transform.localPosition = ItemPosition.GetPosition(Singleton<IDManager>.Instance.ProductSO(a_productId).GridLayoutInStorage, 6);
                product.transform.localRotation = Quaternion.Euler(Singleton<IDManager>.Instance.ProductSO(a_productId).GridLayoutInStorage.productAngles);
                product.transform.localScale = Vector3.one * Singleton<IDManager>.Instance.ProductSO(a_productId).GridLayoutInStorage.scaleMultiplier;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Failed to GetProduct(" + a_productId + ")", ex);
            }

            //LoggerInstance.Msg("Got product " + GetProductName(a_productId));
            return product;
        }

        private string GetProductName(int a_productId)
        {
            if (m_localizationManager == null)
            {
                LoggerInstance.Error("Localization Manager is null");
                return a_productId.ToString();
            }

            return m_localizationManager.LocalizedProductName(a_productId);
        }

        private IEnumerator AddProductsToSlotCR(DisplaySlot a_displaySlot)
        {
            while (!a_displaySlot.Full)
            {
                Product product = GetProduct(a_displaySlot.ProductID, a_displaySlot.transform);

                if (product != null && Helper.TryGetField<DisplaySlot, Label>(a_displaySlot, "m_Label", out Label label) &&
                    Helper.TryGetField<DisplaySlot, ItemQuantity>(a_displaySlot, "m_ProductCountData",
                        out ItemQuantity productCountData) &&
                    Helper.TryGetField<DisplaySlot, List<Product>>(a_displaySlot, "m_Products", out List<Product> products) &&
                    Helper.TryGetField<DisplaySlot, Highlightable>(a_displaySlot, "m_Highlightable",
                        out Highlightable highlightable))
                {
                    AddProductsToSlot(productCountData, product, a_displaySlot, products, highlightable, label);
                    yield return new WaitForSeconds(0.25f);
                }
            }
        }

        private void AddProductsToSlot(ItemQuantity productCountData, Product product, DisplaySlot slot, List<Product> products,
            Highlightable highlightable, Label label)
        {
            Vector3 position =
                ItemPosition.GetPosition(
                    Singleton<IDManager>.Instance.ProductSO(productCountData.FirstItemID).GridLayoutInStorage,
                    productCountData.FirstItemCount);
            product.transform.SetParent(slot.transform);
            product.transform.DOLocalJump(position, 0.3f, 1, 0.3f, false);
            product.transform.DOLocalRotateQuaternion(
                Quaternion.Euler(Singleton<IDManager>.Instance.ProductSO(productCountData.FirstItemID).GridLayoutInStorage
                    .productAngles), 0.3f);
            product.transform.DOScale(
                Singleton<IDManager>.Instance.ProductSO(productCountData.FirstItemID).GridLayoutInStorage.scaleMultiplier,
                0.2f);
            products.Add(product);
            highlightable.AddOrRemoveRenderer(product.GetComponentsInChildren<Renderer>(true), true);
            ItemQuantity productCountDataRef = productCountData;
            int firstItemCount = productCountDataRef.FirstItemCount;
            productCountDataRef.FirstItemCount = firstItemCount + 1;
            label.ProductCount = productCountData.FirstItemCount;

            SetDisplaySlotPrice(productCountData.FirstItemID, slot);
        }

        private void SetDisplaySlotPrice(int a_productID, DisplaySlot a_slot)
        {
            if (Helper.TryGetField<DisplaySlot, PriceTag>(a_slot, "m_PriceTag", out PriceTag priceTag))
            {
                float currentPrice = Singleton<PriceManager>.Instance.SellingPrice(a_productID);
                Helper.TrySetField<DisplaySlot, float>(a_slot, "m_CurrentPrice", currentPrice);
                if (!priceTag.Enabled)
                {
                    priceTag.EnableTag(a_slot);
                }
            }
            else
            {
                LoggerInstance.Error("Failed to get PriceTag");
            }
        }

        private float CalculateOptimalProfitRate(int a_productID)
        {
            ProductSO productSO = Singleton<IDManager>.Instance.ProductSO(a_productID);
            float optimalProfitRate = productSO.OptimumProfitRate;

            // Assume you have a way to iterate or interpolate between minimum and maximum profit rates.
            float minProfitRate = optimalProfitRate; // Define based on your pricing model or productSO properties
            float maxProfitRate = productSO.MaxProfitRate; // Define based on your pricing model or productSO properties
            float step = (maxProfitRate - minProfitRate) / 100; // Define a reasonable step value for iterating profit rates

            float currentPurchaseChance = 0f;

            for (float currentProfitRate = minProfitRate; currentProfitRate < maxProfitRate; currentProfitRate += step)
            {

                // Directly calculate purchase chance for the currentProfitRate, if possible
                float purchaseChance = CalculatePurchaseChance(a_productID, currentProfitRate, out PricingState pricingState); // This needs to be implemented

                //LoggerInstance.Msg($"Pricing State: {pricingState} CurrentProfitRate: {currentProfitRate} Purchase Chance: {purchaseChance} Step: {step}");

                if (pricingState < PricingState.MAXED && pricingState > PricingState.LOSS && currentPurchaseChance <= purchaseChance)
                {
                    currentPurchaseChance = purchaseChance;
                    optimalProfitRate = currentProfitRate;
                }

                //// Calculate expected profit for this profit rate
                //float expectedProfit = currentProfitRate * purchaseChance;

                //// Check if this is the maximum found so far
                //if (expectedProfit > maxProfit && pricingState < PricingState.EXPENSIVE)
                //{
                //    maxProfit = expectedProfit;
                //    optimalProfitRate = currentProfitRate;
                //}
            }

            return optimalProfitRate;
        }

        public float CalculatePurchaseChance(int a_productID, float a_currentProfitRate, out PricingState o_pricingState)
        {
            o_pricingState = PricingState.LOSS;

            ProductSO productSO = Singleton<IDManager>.Instance.ProductSO(a_productID);

            if (!Helper.TryGetField<PriceEvaluationManager, AnimationCurve>(Singleton<PriceEvaluationManager>.Instance,
                    "m_PurchaseChanceCurveForExpensivePrice", out AnimationCurve purchaseChanceCurveForExpensivePrice) ||
                !Helper.TryGetField<PriceEvaluationManager, AnimationCurve>(Singleton<PriceEvaluationManager>.Instance,
                    "m_PurchaseChanceCurveForCheapPrice", out AnimationCurve purchaseChanceCurveForCheapPrice))
            {
                throw new Exception("Failed to get m_PurchaseChanceCurveForExpensivePrice or m_PurchaseChanceCurveForCheapPrice");
            }

            o_pricingState = GetPricingState(a_productID, a_currentProfitRate);
            switch (o_pricingState)
            {
                case PricingState.LOSS:
                    return 200f;
                case PricingState.CHEAP:
                {
                    float time = Mathf.InverseLerp(0f, productSO.OptimumProfitRate, a_currentProfitRate);
                    return purchaseChanceCurveForCheapPrice.Evaluate(time);
                }
                case PricingState.EXPENSIVE:
                {
                    float time = Mathf.InverseLerp(productSO.OptimumProfitRate, productSO.MaxProfitRate, a_currentProfitRate);
                    return purchaseChanceCurveForExpensivePrice.Evaluate(time);
                }
                case PricingState.MAXED:
                    return 0f;
                default:
                    return 0f;
            }
        }

        private PricingState GetPricingState(int a_productID, float a_profitRate)
        {
            PriceManager priceManager = Singleton<PriceManager>.Instance;
            float currentCost = priceManager.CurrentCost(a_productID);

            float price = (float)Math.Round((double)(currentCost + (currentCost * (a_profitRate / 100f))), 2);
            if (price < priceManager.AverageCost(a_productID))
            {
                return global::PricingState.LOSS;
            }
            ProductSO productSO = Singleton<IDManager>.Instance.ProductSO(a_productID);
            float optimumPrice = (float)Math.Round((double)(currentCost + currentCost * productSO.OptimumProfitRate / 100f), 2);
            float maxPrice = (float)Math.Round((double)(currentCost + currentCost * productSO.MaxProfitRate / 100f), 2);
            if (price < optimumPrice)
            {
                return global::PricingState.CHEAP;
            }
            if (price < maxPrice)
            {
                return global::PricingState.EXPENSIVE;
            }
            return global::PricingState.MAXED;
        }

        private float CalculateProfitRate(int a_productID)
        {
            float sellingPrice = Singleton<PriceManager>.Instance.SellingPrice(a_productID);
            float currentCost = Singleton<PriceManager>.Instance.CurrentCost(a_productID);
            return CalculateProfitRate(sellingPrice, currentCost);
        }

        private float CalculateProfitRate(float a_sellingPrice, float a_currentCost)
        {
            return (a_sellingPrice - a_currentCost) * 100f / a_currentCost;
        }

        #endregion
    }
}
