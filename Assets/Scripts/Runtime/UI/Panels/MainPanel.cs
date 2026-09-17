using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace WitchTrial.UI.Panels
{
    /// <summary>
    /// Main-title view adapter. Game-start/load/gallery behavior stays outside
    /// the UI framework and is exposed as UnityEvents.
    /// </summary>
    public sealed class MainPanel : UIPanel
    {
        [Header("Buttons")]
        [FormerlySerializedAs("LoadButton")]
        [SerializeField] private Button loadButton;
        [FormerlySerializedAs("NewGameButton")]
        [SerializeField] private Button newGameButton;
        [FormerlySerializedAs("GalleryButton")]
        [SerializeField] private Button galleryButton;
        [FormerlySerializedAs("OptionsButton")]
        [SerializeField] private Button optionsButton;
        [FormerlySerializedAs("ExitButton")]
        [SerializeField] private Button exitButton;

        public override UILayer Layer => UILayer.Fullscreen;

        private void OnEnable()
        {
            AddListeners();
        }

        private void OnDisable()
        {
            RemoveListeners();
        }

        public void OpenOptions()
        {
            UIRouter.Open<OptionsPanel>();
        }

        public void ExitGame()
        {
            UIRouter.Open<ConfirmPopUp>(new ConfirmContext
            {
                Message = "going to end the game",
                ConfirmText =  "End",
                OnConfirmed =new Action(Application.Quit)
            });
        }

        private void AddListeners()
        {
            loadButton?.onClick.AddListener(InvokeLoadGame);
            newGameButton?.onClick.AddListener(InvokeNewGame);
            galleryButton?.onClick.AddListener(InvokeOpenGallery);
            optionsButton?.onClick.AddListener(OpenOptions);
            exitButton?.onClick.AddListener(ExitGame);
        }

        private void RemoveListeners()
        {
            loadButton?.onClick.RemoveListener(InvokeLoadGame);
            newGameButton?.onClick.RemoveListener(InvokeNewGame);
            galleryButton?.onClick.RemoveListener(InvokeOpenGallery);
            optionsButton?.onClick.RemoveListener(OpenOptions);
            exitButton?.onClick.RemoveListener(ExitGame);
        }

        private void InvokeLoadGame()
        {
            UIRouter.Open<SaveAndLoadPanel>();
        }

        private void InvokeNewGame()
        {
            
        }

        private void InvokeOpenGallery()
        {
            
        }
    }
}
