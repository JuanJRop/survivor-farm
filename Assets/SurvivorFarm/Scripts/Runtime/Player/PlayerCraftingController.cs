using System;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerCraftingController : MonoBehaviour
    {
        [SerializeField] private int storageLevel;
        [SerializeField] private int campLevel;

        private PlayerInventory inventory;
        private PlayerSurvivalStats survivalStats;
        private Sprite squareSprite;
        private Sprite circleSprite;
        private GameObject storageVisual;
        private GameObject campVisual;

        public int StorageLevel => storageLevel;
        public int CampLevel => campLevel;

        public event Action CraftingChanged;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            survivalStats = GetComponent<PlayerSurvivalStats>();
        }

        public void Configure(Sprite square, Sprite circle)
        {
            squareSprite = square;
            circleSprite = circle;
            EnsureVisuals();
            RefreshVisuals();
        }

        public void CraftStorage()
        {
            if (!TryPay(8 + storageLevel * 6, 4 + storageLevel * 3))
            {
                return;
            }

            storageLevel++;
            inventory?.IncreaseSeedCapacity(10);
            RefreshVisuals();
            FarmNotificationCenter.Show($"Almacen construido Nv.{storageLevel}. Mas espacio para semillas.");
            CraftingChanged?.Invoke();
        }

        public void CraftCamp()
        {
            if (!TryPay(6 + campLevel * 5, 6 + campLevel * 5))
            {
                return;
            }

            campLevel++;
            survivalStats?.IncreaseMaxHealth(1);
            RefreshVisuals();
            FarmNotificationCenter.Show($"Campamento mejorado Nv.{campLevel}. +1 vida maxima.");
            CraftingChanged?.Invoke();
        }

        public string GetCraftingSummary()
        {
            return $"Almacen Nv.{storageLevel} | Campamento Nv.{campLevel}";
        }

        public void Restore(int savedStorageLevel, int savedCampLevel)
        {
            storageLevel = Mathf.Max(0, savedStorageLevel);
            campLevel = Mathf.Max(0, savedCampLevel);
            RefreshVisuals();
            CraftingChanged?.Invoke();
        }

        private bool TryPay(int woodCost, int stoneCost)
        {
            if (inventory == null)
            {
                return false;
            }

            if (inventory.Wood < woodCost || inventory.Stone < stoneCost)
            {
                FarmNotificationCenter.Show($"Falta material: {woodCost} madera y {stoneCost} piedra.");
                return false;
            }

            inventory.TryRemoveWood(woodCost);
            inventory.TryRemoveStone(stoneCost);
            return true;
        }

        private void EnsureVisuals()
        {
            if (squareSprite == null || circleSprite == null)
            {
                return;
            }

            if (storageVisual == null)
            {
                storageVisual = CreateStructureVisual("Storage Structure", new Vector3(-1.55f, -6.85f, 0f), new Color(0.48f, 0.30f, 0.16f));
            }

            if (campVisual == null)
            {
                campVisual = CreateStructureVisual("Camp Structure", new Vector3(1.55f, -6.85f, 0f), new Color(0.74f, 0.36f, 0.16f));
            }
        }

        private GameObject CreateStructureVisual(string name, Vector3 position, Color color)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            SpriteRenderer baseRenderer = root.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = squareSprite;
            baseRenderer.color = color;
            baseRenderer.sortingOrder = 1;
            root.transform.localScale = new Vector3(0.9f, 0.75f, 1f);

            GameObject marker = new GameObject("Marker");
            marker.transform.SetParent(root.transform);
            marker.transform.localPosition = new Vector3(0f, 0.44f, -0.01f);
            marker.transform.localScale = Vector3.one * 0.28f;

            SpriteRenderer markerRenderer = marker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = circleSprite;
            markerRenderer.color = Color.white;
            markerRenderer.sortingOrder = 2;
            return root;
        }

        private void RefreshVisuals()
        {
            EnsureVisuals();

            if (storageVisual != null)
            {
                storageVisual.SetActive(storageLevel > 0);
            }

            if (campVisual != null)
            {
                campVisual.SetActive(campLevel > 0);
            }
        }
    }
}
