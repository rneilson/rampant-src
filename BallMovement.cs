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
	public GameObject muzzleFlash;
	public float muzzleDuration;
	public GameObject deathThroes;
	public GameObject powerUp;
	public GameObject powerUpBoom;
	public GameObject bombMinusTwo;
	public GameObject bombMinusOne;
	public GameObject bombAcquired;
	public GameObject bombOnBoard;
	private GameObject bombBlinker;
	private GameObject fireCursor;
	private CursorMovement fireCursorControl;
	//public AudioClip boomKillSound;
	
	private int fireMode = 1;
	private int fireCycle = 0;
	private Vector3 firePosCurrent;
	private Vector3 fireDirCurrent;
	private float bulletLifetime = 1.5f;
	
	private const float piOverFour = Mathf.PI / 4;

	private GameObject controller;
	//private Scorer scorer;

	const float fireDist = 0.10f;
	const float fireSpeed = 5.0f;
	public float bombHeight = 0.51f;
	public float bombForce = 500f;
	public float bombKillRadius = 2.0f;
	public float bombPushRadius = 4.5f;

	// Status vars
	private bool biggerGun;
	private bool hasBomb;
	private bool triggerDown;

	// Status properties
	public bool BiggerGun {
		get {
			if(fireMode < 2) {
				return false;
			}
			else {
				return true;
			}
		}
	}
	public bool HasBomb {
		get { return hasBomb; }
	}
	
	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();

		myRigidbody.drag=dragNoInput;
		controller = GameObject.FindGameObjectWithTag("GameController");
		fireCursor = GameObject.Find("FireCursor");
		fireCursorControl = fireCursor.GetComponent<CursorMovement>();
		//scorer = controller.GetComponent<Scorer>();
	}
	
	// Update is called once per frame
	void Update () {
		// Check if bomb triggered
		if (hasBomb) {
			if (Input.GetButtonDown("BombButton")) {
				UseBomb();
			}
			if (Mathf.Abs(Input.GetAxis("BombTrigger")) > 0.05f) {
				if (!triggerDown) {
					triggerDown = true;
					UseBomb();
				}
			}
			else {
				triggerDown = false;
			}
		}
	}
	
	//Put everything in FixedUpdate
	void FixedUpdate () {
		// Get movement
		float dx=Input.GetAxis("Horizontal");
		float dz=Input.GetAxis("Vertical");
		float ndx = 0.0f;
		float ndz = 0.0f;
		float normalFactor = Mathf.Sqrt(dx*dx + dz*dz); 

		//Normalize if input vector length > 1.0, keep within unit circle
		if(normalFactor > 1.0f) {
			ndx = (dx/normalFactor)*movementScale;
			ndz = (dz/normalFactor)*movementScale;
		}
		//Otherwise use input vector as-is
		else {
			ndx = dx*movementScale;
			ndz = dz*movementScale;
		}
		
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
		
		// Get right stick input
		float dxr = Input.GetAxis("RightHorizontal");
		float dzr = Input.GetAxis("RightVertical");

		// If right stick moved, shoot in that direction
		if(dxr != 0 || dzr != 0) {
			// Hide mouse cursor
			fireCursorControl.SendMessage("HideCursor");

			// Fire in given direction
			//fireDirCurrent = new Vector3(dxr, 0, dzr).normalized;
			FireGun(new Vector3(dxr, 0, dzr).normalized);
		}
		// Otherwise, check if left mouse pressed and fire towards cursor
		else if (Input.GetButton("Fire")) {
			// Show mouse cursor
			fireCursorControl.SendMessage("UnhideCursor");

			// Fire in given direction
			FireGun(new Vector3((fireCursor.transform.position.x - transform.position.x), 0, 
				(fireCursor.transform.position.z - transform.position.z)).normalized);
		}

		// Check if bomb triggered
		/*if (hasBomb) {
			if (Input.GetButton("BombButton")) {
				UseBomb();
			}
			if (Mathf.Abs(Input.GetAxis("BombTrigger")) > 0.05f) {
				UseBomb();
			}
		}*/
		
		// Debug guitext
		//xInput.text = dxr.ToString();
		//zInput.text = dzr.ToString();
	}
	
	void BlowUp () {
		controller.SendMessage("PlayerDied");
		Destroy(Instantiate(deathThroes, transform.position, Quaternion.Euler(-90, 0, 0)), 1f);
		Destroy(gameObject);
	}
	
	// Fire bullet
	void FireBullet (Vector3 firePos, Vector3 fireDir, float speed, AudioClip fireSound) {
		//alternating *= -1f;
		//Vector3 dir = new Vector3(dirx, 0, dirz).normalized;
		//Vector3 firePos = transform.position + (new Vector3(alternating*dirz, 0, alternating*dirx*-1f).normalized * 0.15f);

		//Rigidbody bulletClone = (Rigidbody) Instantiate(bullet, firePos, transform.rotation);
		Rigidbody bulletClone = Instantiate(bullet, firePos, transform.rotation) as Rigidbody;
		//Destroy(bulletClone, bulletLifetime);
		bulletClone.AddForce(fireDir * speed);
		myAudioSource.PlayOneShot(fireSound, 1.0f);

		// Muzzle flash
		if (muzzleFlash) {
			Destroy(Instantiate(muzzleFlash, firePos, transform.rotation), muzzleDuration);
		}
	}
	
	Vector3 RotateFortyFive (Vector3 vec, float dir) {
		// dir 1 is counterclockwise, -1 is clockwise
		float newx = vec.x*Mathf.Cos(piOverFour) - vec.z*Mathf.Sin(piOverFour)*dir;
		float newz = vec.x*Mathf.Sin(piOverFour)*dir + vec.z*Mathf.Cos(piOverFour);
		return new Vector3(newx, 0, newz);
	}
	
	void FireGun (Vector3 fireDir) {
		// Check firemode and fire accordingly
		if (fireMode == 1) {
			if (fireCycle == 0) {
				firePosCurrent = transform.position + (fireDir * fireDist);
				FireBullet(firePosCurrent, fireDir, fireSpeed, bulletSound1);
			}
		}
		else if (fireMode == 2) {
			if (fireCycle == 0) {
				//firePosCurrent = transform.position + (new Vector3(dzr, 0, -1f*dxr).normalized * 0.15f);
				firePosCurrent  = transform.position + (RotateFortyFive(fireDir, 1.0f) * fireDist);
				FireBullet(firePosCurrent, fireDir, fireSpeed, bulletSound2);
			}
			else if (fireCycle == 3) {
				//firePosCurrent = transform.position + (new Vector3(-1f*dzr, 0, dxr).normalized * 0.15f);
				firePosCurrent  = transform.position + (RotateFortyFive(fireDir, -1.0f) * fireDist);
				FireBullet(firePosCurrent, fireDir, fireSpeed, bulletSound3);
			}
		}
		// Removed while testing firemode 2 limit
		/* else if (fireMode == 3) {
			if (fireCycle == 0) {
				firePosCurrent = transform.position + (new Vector3(dzr, 0, -1f*dxr).normalized * 0.15f);
				FireBullet(firePosCurrent, fireDirCurrent, fireSpeed, bulletSound2);
			}
			else if (fireCycle == 2) {
				firePosCurrent = transform.position + (new Vector3(dxr, 0, dzr).normalized * 0.15f);
				FireBullet(firePosCurrent, fireDirCurrent, fireSpeed, bulletSound1);
			}
			else if (fireCycle == 4) {
				firePosCurrent = transform.position + (new Vector3(-1f*dzr, 0, dxr).normalized * 0.15f);
				FireBullet(firePosCurrent, fireDirCurrent, fireSpeed, bulletSound3);
			}
		} */
		
		// Advance firing cycle
		fireCycle++;
		if (fireCycle > 5){
			fireCycle = 0;
		}
	}

	public void FireFaster () {
		if (fireMode < 2) {
			fireMode++;
			PowerUp();
		}
		//else
		//	PowerUpBoom();
	}
	
	void PowerUp () {
		//Destroy(Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		GameObject powerUpEffect = Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
		powerUpEffect.transform.parent = transform;
		Destroy(powerUpEffect, 0.5f);
	}
	
	public void BombMinusTwo () {
		//Destroy(Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		if (bombMinusTwo) {
			GameObject powerUpEffect = Instantiate(bombMinusTwo, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
			powerUpEffect.transform.parent = transform;
			Destroy(powerUpEffect, 0.5f);
		}
	}

	public void BombMinusOne () {
		//Destroy(Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		if (bombMinusOne) {
			GameObject powerUpEffect = Instantiate(bombMinusOne, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
			powerUpEffect.transform.parent = transform;
			Destroy(powerUpEffect, 0.5f);
		}
	}

	public void GiveBomb (bool forced) {
		hasBomb = true;
		if (!forced) {
			// Powerup effect (transitory)
			if (bombAcquired) {
				GameObject powerUpEffect = Instantiate(bombAcquired, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
				powerUpEffect.transform.parent = transform;
				Destroy(powerUpEffect, 0.5f);
			}

			// Bomb-carrying effect (permanent until bomb used)
			if (bombOnBoard) {
				bombBlinker = Instantiate(bombOnBoard, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
				bombBlinker.transform.parent = transform;
			}
		}
	}

	// Old name: void PowerUpBoom () {
	public void UseBomb () {
		Vector3 bombPos = new Vector3 (transform.position.x, bombHeight, transform.position.z);

		// Use up bomb
		hasBomb = false;
		// Destroy armed-bomb effect
		Destroy(bombBlinker, 0.0f);

		// Spawn effect
		// At player position because it looks better
		Destroy(Instantiate(powerUpBoom, transform.position, Quaternion.Euler(0, 0, 0)), 1.0f);
		
		Collider[] enemies;
		int mask = 1 << LayerMask.NameToLayer("Enemy");
		
		// Kill enemies in inner radius
		enemies = Physics.OverlapSphere(bombPos, bombKillRadius, mask);
		if (enemies.Length > 0) {
			for (int i=0; i<enemies.Length; i++) {
				enemies[i].SendMessage("Die", false);
			}
		}
		
		// Push enemies in outer radius
		enemies = Physics.OverlapSphere(bombPos, bombPushRadius, mask);
		for (int i=0; i<enemies.Length; i++) {
			enemies[i].GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
		}
	}

	public void Die (bool loudly) {
		BlowUp();
	}
	
	void OnCollisionEnter (Collision collision) {
		GameObject thingHit = collision.gameObject;
		if (thingHit.tag == "Enemy") {
			BlowUp();
		}

	}
}
