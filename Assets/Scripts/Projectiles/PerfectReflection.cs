using UnityEngine;

public class PerfectReflection : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 lastFrameVelocity;

    // 最低限の速度を保証したい場合（止まってしまうのを防ぐ）
    public float minSpeed = 10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 衝突した瞬間の計算用に、フレームごとの速度を記録しておく
        // (OnCollisionEnterが呼ばれた時点では、すでに物理演算で速度が変わってしまっているため)
        lastFrameVelocity = rb.velocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1. 衝突点における壁の「法線（面の向き）」を取得
        Vector3 normal = collision.contacts[0].normal;

        // 2. 入射ベクトル(衝突前の速度)と法線から、反射ベクトルを計算
        // Vector3.Reflect(入射方向, 法線)
        Vector3 reflectDir = Vector3.Reflect(lastFrameVelocity.normalized, normal);

        // 3. 速度を決定（元の速度を維持するか、一定速度にするか）
        // ここでは「衝突前の速さを維持」します
        float speed = lastFrameVelocity.magnitude;

        // もし減速を防ぎたいなら、以下の行のコメントを外して最低速度を保証する
        // speed = Mathf.Max(speed, minSpeed);

        // 4. 新しい速度をRigidbodyに強制適用
        rb.velocity = reflectDir * speed;
    }
}