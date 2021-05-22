using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace TaleSpireUnofficalPlugins
{
    [BepInPlugin(Guid, "Internet Server Plug-In", Version)]
    public partial class InternetServerPlugin : BaseUnityPlugin
    {
        // Plugin Guid
        public const string Guid = "3fef4757-5c0e-4988-a04b-d2c355d648d6";
        public const string Version = "1.0.0.0";

        /// <summary>
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("Internet Server Plugin ready");
        }

        /// <summary>
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            // Ensure that there is a camera controller instance
            if (CameraController.HasInstance)
            {
                // Ensure that there is a board session manager instance
                if (BoardSessionManager.HasInstance)
                {
                    // Ensure that there is a board
                    if (BoardSessionManager.HasBoardAndIsInNominalState)
                    {
                        // Ensure that the board is not loading
                        if (!BoardSessionManager.IsLoading)
                        {
                        }
                    }
                }
            }
        }
    }
}
