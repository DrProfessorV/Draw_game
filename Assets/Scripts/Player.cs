using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public float speed = 10f;

    private float lastSynchronizationTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Vector3 syncStartPosition = Vector3.zero;
    private Vector3 syncEndPosition = Vector3.zero;
	private Quaternion syncStartRotation = Quaternion.identity;
	private Quaternion syncEndRotation = Quaternion.identity;

	//This function is for syncing all the player movement to the server
    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        Vector3 syncPosition = Vector3.zero;
        Vector3 syncVelocity = Vector3.zero;
		Quaternion syncRotation = Quaternion.identity;

        if (stream.isWriting)
        {
            syncPosition = rigidbody.position;
            stream.Serialize(ref syncPosition);

            syncVelocity = rigidbody.velocity;
            stream.Serialize(ref syncVelocity);

			syncRotation = rigidbody.rotation;
			stream.Serialize(ref syncRotation);
        }
        else
        {
            stream.Serialize(ref syncPosition);
            stream.Serialize(ref syncVelocity);
			stream.Serialize(ref syncRotation);

            syncTime = 0f;
            syncDelay = Time.time - lastSynchronizationTime;
            lastSynchronizationTime = Time.time;

            syncEndPosition = syncPosition + syncVelocity * syncDelay;
            syncStartPosition = rigidbody.position;

			syncStartRotation = rigidbody.rotation;
        }
    }
	//On game startup
    void Awake()
    {
        lastSynchronizationTime = Time.time;
    }
	//If this is the player object your network is linked to, give controls
    void Update()
    {
        if (networkView.isMine)
        {
            InputMovement();
            InputColorChange();
        }
        else
        {
            //SyncedMovement();
        }
    }

	//Basic movement
    private void InputMovement()
    {
        if (Input.GetKey(KeyCode.W))
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Input.GetKey(KeyCode.S))
            transform.Translate(-Vector3.forward * speed * Time.deltaTime);

        if (Input.GetKey(KeyCode.D))
			transform.Translate(Vector3.right * speed * Time.deltaTime);

        if (Input.GetKey(KeyCode.A))
			transform.Translate(-Vector3.right * speed * Time.deltaTime);
    }

	//Movement for other players in a lobby
    private void SyncedMovement()
    {
        syncTime += Time.deltaTime;
        rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
		rigidbody.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
    }

	//Change the color of the player
    private void InputColorChange()
    {
        if (Input.GetKeyDown(KeyCode.R))
            ChangeColorTo(new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
    }

    [RPC] void ChangeColorTo(Vector3 color)
    {
        renderer.material.color = new Color(color.x, color.y, color.z, 1f);

        if (networkView.isMine)
            networkView.RPC("ChangeColorTo", RPCMode.OthersBuffered, color);
    }
}
