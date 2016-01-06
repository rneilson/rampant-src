using UnityEngine;
using System.Collections;

public class BallMovement : MonoBehaviour {
	
	public float movementScale;
	public float dragNoInput;
	public float dragInput;

	private AudioSource myAudioSource;
	public AudioClip bulletSound1;
	public AudioClip bulletSound2;
	public AudioClip bulletSound3;
	
	private Rigidbody myRigidbody;
	public Rigidbody bullet;
	public GameObject deathThroes;
	public GameObject powerUp;
	public GameObject powerUpBoom;
	//public AudioClip boomKillSound;
	
	private int fireMode = 1;
	private int fireCycle = 0;
	private Vector3 firePosCurrent;
	private Vector3 fireDirCurrent;
	
	private const float piOverFour = Mathf.PI / 4;

	private GameObject controller;
	//private Scorer scorer;

	const float fireDist = 0.16f;
	
	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();

		myRigidbody.drag=dragNoInput;
		controller = GameObject.FindGameObjectWithTag("GameController");
		//scorer = controller.GetComponent<Scorer>();
	}
	
	// Fire bullet
	void FireBullet (Vector3 firePos, Vector3 fireDir, float speed, AudioClip fireSound) {
		//alternating *= -1f;
		//Vector3 dir = new Vector3(dirx, 0, dirz).normalized;
		//Vector3 firePos = transform.position + (new Vector3(alternating*dirz, 0, alternating*dirx*-1f).normalized * 0.15f);

		//Rigidbody bulletClone = (Rigidbody) Instantiate(bullet, firePos, transform.rotation);
		Rigidbody bulletClone = Instantiate(bullet, firePos, transform.rotation) as Rigidbody;
		bulletClone.AddForce(fireDir * speed);
		myAudioSource.PlayOneShot(fireSound, 1.0f);
	}
	
	Vector3 RotateFortyFive (Vector3 vec, float dir) {
		// dir 1 is counterclockwise, -1 is clockwise
		float newx = vec.x*Mathf.Cos(piOverFour) - vec.z*Mathf.Sin(piOverFour)*dir;
		float newz = vec.x*Mathf.Sin(piOverFour)*dir + vec.z*Mathf.Cos(piOverFour);
		return new Vector3(newx, 0, newz);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	//Put everything in FixedUpdate
	void FixedUpdate () {
		float dx=Input.GetAxis("Horizontal");
		float dz=Input.GetAxis("Vertical");
		float normalFactor = Mathf.Sqrt(dx*dx + dz*dz); //Normalize!
		//float normalFactor = 1;
		float ndx = dx/normalFactor*movementScale*Mathf.Abs(dx);
		float ndz = dz/normalFactor*movementScale*Mathf.Abs(dz);
		
		if(dx == 0 && dz == 0) {
			// Drag enabled
			myRigidbody.drag = dragNoInput;
		}
		else {
			// Drag enabled, but slower
			myRigidbody.drag = dragInput;
			// Give the ball a push
			myRigidbody.AddForce(ndx, 0, ndz);

		}
		
		// Bullet launcher setup
		float dxr = Input.GetAxis("RightHorizontal");
		float dzr = Input.GetAxis("RightVertical");

		if(dxr != 0 || dzr != 0)
		{
			fireDirCurrent = new Vector3(dxr, 0, dzr).normalized;
			
			if (fireMode == 1) {
				if (fireCycle == 0) {
					firePosCurrent = transform.position + (fireDirCurrent * fireDist);
					FireBullet(firePosCurrent, fireDirCurrent, 5f, bulletSound1);
				}
			}
			else if (fireMode == 2) {
				if (fireCycle == 0) {
					//firePosCurrent = transform.position + (new Vector3(dzr, 0, -1f*dxr).normalized * 0.15f);
					firePosCurrent  = transform.position + (RotateFortyFive(fireDirCurrent, 1.0f) * fireDist);
					FireBullet(firePosCurrent, fireDirCurrent, 5f, bulletSound2);
				}
				else if (fireCycle == 3) {
					//firePosCurrent = transform.position + (new Vector3(-1f*dzr, 0, dxr).normalized * 0.15f);
					firePosCurrent  = transform.position + (RotateFortyFive(fireDirCurrent, -1.0f) * fireDist);
					FireBullet(firePosCurrent, fireDirCurrent, 5f, bulletSound3);
				}
			}
			// Removed while testing firemode 2 limit
			/* else if (fireMode == 3) {
				if (fireCycle == 0) {
					firePosCurrent = transform.position + (new Vector3(dzr, 0, -1f*dxr).normalized * 0.15f);
					FireBullet(firePosCurrent, fireDirCurrent, 5f, bulletSound2);
				}
				else if (fireCycle == 2) {
					firePosCurrent = transform.position + (new Vector3(dxr, 0, dzr).normalized * 0.15f);
					FireBullet(firePosCurrent, fireDirCurrent, 5f, bulletSound1);
				}
				else if (fireCycle == 4) {
					firePosCurrent = transform.position + (new Vector3(-1f*dzr, 0, dxr).normalized * 0.15f);
					FireBullet(firePosCurrent, fireDirCurrent, 5f, bulletSound3);
				}
			} */
			
			fireCycle++;
			if (fireCycle > 5)
				fireCycle = 0;
				
			// Old fire delay code
			/* if (delayPortion == 0) {
				// Fire!
				FireBullet(dxr, dzr, 5f);
				delayPortion = fireDelay;
			}
			else {
				delayPortion--;
			} */
		}
		
		// Debug guitext
		//xInput.text = dxr.ToString();
		//zInput.text = dzr.ToString();
	}
	
	void BlowUp () {
		controller.SendMessage("PlayerDied");
		Destroy(Instantiate(deathThroes, transform.position, Quaternion.Euler(-90, 0, 0)), 1f);
		Destroy(gameObject);
	}
	
	void FireFaster () {
		if (fireMode < 2) {
			fireMode++;
			PowerUp();
		}
		else
			PowerUpBoom();
	}
	
	void PowerUp () {
		Destroy(Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)), 1.0f);
	}
	
	void PowerUpBoom () {
		//Old bomb code
		/* GameObject[] enemies;
		enemies = GameObject.FindGameObjectsWithTag("Enemy"); */

		// Spawn effect
		Destroy(Instantiate(powerUpBoom, transform.position, Quaternion.Euler(0, 0, 0)), 1.0f);
		
		Collider[] enemies;
		int mask = 1 << LayerMask.NameToLayer("Enemy");
		
		// Kill enemies in inner radius
		enemies = Physics.OverlapSphere(transform.position, 2.0f, mask);
		if (enemies.Length > 0) {
			for (int i=0; i<enemies.Length; i++) {
				enemies[i].SendMessage("Die", false);
			}
			//scorer.AddKills(enemies.Length);
			//controller.audio.PlayOneShot(boomKillSound);
		}
		
		// Push enemies in outer radius
		enemies = Physics.OverlapSphere(transform.position, 4.0f, mask);
		for (int i=0; i<enemies.Length; i++) {
			enemies[i].GetComponent<Rigidbody>().AddExplosionForce(500f, transform.position, 0f);
		}

		/* for (int i = 0; i < enemies.Length; i++) {
			var enemy = enemies[i];
				
			if ((enemy.transform.position - transform.position).magnitude < 4.0f) {
				enemy.rigidbody.AddExplosionForce(1000f, transform.position, 4.0f);
				//enemy.SendMessage("BlowUp", true);
			}
		} */
	}
	
	void OnCollisionEnter (Collision collision) {
		GameObject thingHit = collision.gameObject;
		if (thingHit.tag == "Enemy") {
			BlowUp();
		}

	}
}
