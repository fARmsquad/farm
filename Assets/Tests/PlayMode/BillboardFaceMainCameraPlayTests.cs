using System.Collections;
using FarmSimVR.MonoBehaviours.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FarmSimVR.Tests.PlayMode
{
    public sealed class BillboardFaceMainCameraPlayTests
    {
        private GameObject _cameraGo;
        private GameObject _billboardGo;

        [TearDown]
        public void TearDown()
        {
            if (_billboardGo != null)
                Object.Destroy(_billboardGo);
            if (_cameraGo != null)
                Object.Destroy(_cameraGo);
        }

        [UnityTest]
        public IEnumerator LateUpdate_AlignsForwardWithMainCamera()
        {
            _cameraGo = new GameObject("TestMainCamera");
            _cameraGo.tag = "MainCamera";
            var cam = _cameraGo.AddComponent<Camera>();
            _cameraGo.transform.SetPositionAndRotation(
                new Vector3(2f, 1f, -4f),
                Quaternion.Euler(10f, 35f, 0f));

            _billboardGo = new GameObject("Billboard");
            _billboardGo.AddComponent<BillboardFaceMainCamera>();

            yield return null;

            Vector3 expected = cam.transform.forward;
            Vector3 actual = _billboardGo.transform.forward;
            Assert.Greater(Vector3.Dot(actual, expected), 1f - 0.0001f,
                "Billboard forward should match Camera.main forward after LateUpdate.");
        }
    }
}
