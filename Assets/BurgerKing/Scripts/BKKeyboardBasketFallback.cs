using UnityEngine;

/// <summary>
/// Kinect yokken / oyuncu takip edilmiyorken sepeti klavye ile (A/D veya ok tuşları) hareket ettirir.
/// Test ve acil durum kontrolü içindir; Kinect oyuncuyu takip ederken devre dışı kalır.
/// </summary>
public class BKKeyboardBasketFallback : MonoBehaviour
{
    public BasketController2D controller;

    [Tooltip("Klavye hareket hızı (birim/sn)")]
    public float speed = 14f;

    [Tooltip("true: yalnızca Kinect oyuncuyu takip edip sepeti kalibre etmişken devre dışı kalır")]
    public bool onlyWhenNoKinectUser = true;

    [Tooltip("Hareket aralığının merkezi (dünya X)")]
    public float centerX = 0f;

    void Update()
    {
        if (!controller || !controller.basket2D) return;

        if (onlyWhenNoKinectUser)
        {
            KinectManager km = KinectManager.Instance;
            bool userTracked = km != null && km.IsInitialized() && km.GetUserIdByIndex(controller.playerIndex) != 0;
            if (userTracked && controller.IsCalibrated) return;
        }

        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Mathf.Approximately(h, 0f)) return;

        Transform t = controller.basket2D;
        float range = controller.horizontalRange;
        float x = Mathf.Clamp(t.position.x + h * speed * Time.deltaTime, centerX - range, centerX + range);
        t.position = new Vector3(x, t.position.y, t.position.z);
    }
}
