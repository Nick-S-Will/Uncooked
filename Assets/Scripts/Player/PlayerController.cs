﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Mob;

namespace Overrailed.Player
{
    [SelectionBase]
    public class PlayerController : HumanoidController
    {
        private PlayerInput playerInput;

        protected static List<PlayerController> players = new List<PlayerController>();

        private void Awake()
        {
            playerInput = new PlayerInput();

            playerInput.Movement.Walk.performed += ctx => InputDir = ctx.ReadValue<Vector2>();
            playerInput.Movement.Walk.canceled += ctx => InputDir = Vector2.zero;
            playerInput.Movement.Dash.started += ctx => AudioManager.instance.PlaySound(dashSound, transform.position);
            playerInput.Movement.Dash.started += ctx => HoldingDashKey = true;
            playerInput.Movement.Dash.canceled += ctx => HoldingDashKey = false;

            playerInput.Interaction.InteractMain.performed += ctx => InteractAll();
            playerInput.Interaction.InteractAlt.performed += ctx => InteractSingle();

            players.Add(this);
        }

        protected override void Start()
        {
            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint += ForceDrop;
                GameManager.instance.OnGameEnd += playerInput.Disable;
                GameManager.instance.OnGameEnd += ForceDrop;
            }
            else if (TutorialManager.Exists)
            {
                var tutorial = FindObjectOfType<TutorialManager>();
                tutorial.OnShowInfo += DisableControls;
                tutorial.OnCloseInfo += EnableControls;
            }
            else if (!MainMenuManager.Exists) Debug.LogError("No Manager Found");

            base.Start();

            DisableControls();
            if (Map)
            {
                Map.OnFinishAnimateChunk += EnableControls;
                if (Map.HighlightEnabled) _ = StartCoroutine(TileHighlighting());
            }
        }

        private IEnumerator TileHighlighting()
        {
            // Tile highlighting
            _ = Physics.Raycast(transform.position + Vector3.up, LastInputDir, out RaycastHit hitInfo, 1, Map.InteractMask);
            var tile = hitInfo.transform;
            if (tile == null) tile = Map.GetTileAt(LookPoint + Vector3Int.down);
            Map.TryHighlightTile(tile);

            yield return null;
        }

        public static float MinDistanceToPlayer(Vector3 point)
        {
            float minDst = float.MaxValue;

            foreach (var player in players)
            {
                float dst = Vector3.Distance(point, player.transform.position);
                if (dst < minDst) minDst = dst;
            }

            return minDst;
        }

        public void EnableControls() => enabled = true;
        public void DisableControls() => enabled = false;
        
        private void OnEnable()
        {
            if (playerInput != null)
            {
                playerInput.Enable();
                _ = StartCoroutine(HandleMovement());
            }
        }
        private void OnDisable()
        {
            if (playerInput != null)
            {
                playerInput.Disable();
                StopMovement();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint -= ForceDrop;
                GameManager.instance.OnGameEnd -= playerInput.Disable;
                GameManager.instance.OnGameEnd -= ForceDrop;
            }

            players.Remove(this);
        }
    }
}