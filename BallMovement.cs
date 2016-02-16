using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BallMovement : MonoBehaviour {
	
	public float movementScale;
	public float dragNoInput;
	public float dragInput;
	private Vector3 prevVel;

	public AudioClip bulletSound;
	public AudioSource[] audioArray;
	
	private Rigidbody myRigidbody;
	public Rigidbody bullet;
	public GameObject muzzleFlash;
	public float muzzleDuration;
	public GameObject deathThroes;
	public GameObject deathFade;
	public GameObject powerUp;
	public GameObject powerUpBoom;
	public GameObject bombMinusTwo;
	public GameObject bombMinusOne;
	public GameObject bombAcquired;
	public GameObject bombOnBoard;
	private GameObject bombBlinker;
	private GameObject fireCursor;
	private CursorMovement fireCursorControl;
	
	private int fireMode = 1;
	private int fireCycle = 0;
	private int audioCycle;
	private int audioCycleMin;
	private int audioCycleMax;
	private Vector3 firePosCurrent;
	private Vector3 fireDirCurrent;
	
	private const float piOverFour = Mathf.PI / 4;
	private const float piOverTwo = Mathf.PI / 2;

	//private GameObject controller;
	private Scorer scorer;
	//private RedCubeGroundControl groundControl;
	private int enemyLayer;

	const float fireDist1 = 0.04f;
	const float fireDist2 = 0.08f;
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
		//myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();

		myRigidbody.drag=dragNoInput;
		//controller = GameObject.FindGameObjectWithTag("GameController");
		fireCursor = GameObject.Find("FireCursor");
		fireCursorControl = fireCursor.GetComponent<CursorMovement>();
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();
		//groundControl = controller.GetComponent<RedCubeGroundControl>();

		// Firing and audio cycle stuff
		// Called to set everything else
		// (fireMode may be set by scorer)
		SetFireMode(fireMode);

		enemyLayer = LayerMask.NameToLayer("Enemy");
	}
	
	// Update is called once per frame
	void Update () {
		// Check if bomb triggered
		if ((hasBomb) && (scorer.InputTarget == InputMode.Game)) {
			if ((Input.GetKeyDown(KeyCode.Mouse1)) 
				|| (Input.GetButtonDown("BombKey"))
				|| (Input.GetButtonDown("BombButton"))) {
				UseBomb();
			}
			if ((Mathf.Abs(Input.GetAxis("BombTrigger")) > 0.05f) 
				|| (Mathf.Abs(Input.GetAxis("BombTriggerL")) > 0.05f) 
				|| (Mathf.Abs(Input.GetAxis("BombTriggerR")) > 0.05f)) {
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
		// Update previous velocity
		prevVel = myRigidbody.velocity;

		// Only move/shoot if input directed to game
		if (scorer.InputTarget == InputMode.Game) {
			// Get movement
			float dx=Input.GetAxis("MoveHorizontal");
			float dz=Input.GetAxis("MoveVertical");
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
			float dxr = Input.GetAxis("FireHorizontal");
			float dzr = Input.GetAxis("FireVertical");

			// If right stick moved, shoot in that direction
			if(dxr != 0 || dzr != 0) {
				// Hide mouse cursor
				fireCursorControl.HideCursor();

				// Fire in given direction
				//fireDirCurrent = new Vector3(dxr, 0, dzr).normalized;
				FireGun(new Vector3(dxr, 0, dzr).normalized);
			}
			// Otherwise, check if left mouse pressed and fire towards cursor
			else if (Input.GetKey(KeyCode.Mouse0)) {
				// Show mouse cursor
				fireCursorControl.UnhideCursor();

				// Fire in given direction
				FireGun(new Vector3((fireCursor.transform.position.x - transform.position.x), 0, 
					(fireCursor.transform.position.z - transform.position.z)).normalized);
			}

		}
	}
	
	void BlowUp () {
		scorer.SendMessage("PlayerDied");
		// Create death explosion of glittering particles
		GameObject death = Instantiate(deathThroes, transform.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
		// Give it our current velocity so the particles can inherit it
		Rigidbody deathRB = death.GetComponent<Rigidbody>();
		deathRB.velocity = prevVel;
		// And now, in a pique of poetry, destroy death
		Destroy(death, 1f);
		// Give us our fading ghost
		Destroy(Instantiate(deathFade, transform.position, Quaternion.Euler(0, 0, 0)), 1f);
		// Oh yeah, now destory ourselves
		Destroy(gameObject);
	}
	
	// Fire bullet
	void FireBullet (Vector3 firePos, Vector3 fireDir, float speed) {
		// Create and launch bullet
		Rigidbody bulletClone = Instantiate(bullet, firePos, Quaternion.Euler(90, 0, 0)) as Rigidbody;
		bulletClone.AddForce(fireDir * speed);

		// Play sound and advance/reset audio source counter
		audioArray[audioCycle].PlayOneShot(bulletSound, 1.0f);
		if (++audioCycle >= audioCycleMax) {
			audioCycle = audioCycleMin;
		}

		// Muzzle flash
		if (muzzleFlash) {
			Destroy(Instantiate(muzzleFlash, transform.position, 
				Quaternion.FromToRotation(Vector3.forward, fireDir)), muzzleDuration);
		}
	}
	
	Vector3 RotateFirePos (Vector3 vec, float dir) {
		// dir 1 is counterclockwise, -1 is clockwise
		float newx = vec.x*Mathf.Cos(piOverTwo) - vec.z*Mathf.Sin(piOverTwo)*dir;
		float newz = vec.x*Mathf.Sin(piOverTwo)*dir + vec.z*Mathf.Cos(piOverTwo);
		return new Vector3(newx, 0, newz);
	}
	
	void FireGun (Vector3 fireDir) {
		// Check firemode and fire accordingly
		if (fireMode == 1) {
			if (fireCycle == 0) {
				firePosCurrent = transform.position + (fireDir * fireDist1);
				FireBullet(firePosCurrent, fireDir, fireSpeed);
			}
		}
		else if (fireMode == 2) {
			if (fireCycle == 0) {
				firePosCurrent  = transform.position + (RotateFirePos(fireDir, 1.0f) * fireDist2);
				FireBullet(firePosCurrent, fireDir, fireSpeed);
			}
			else if (fireCycle == 3) {
				firePosCurrent  = transform.position + (RotateFirePos(fireDir, -1.0f) * fireDist2);
				FireBullet(firePosCurrent, fireDir, fireSpeed);
			}
		}
		
		// Advance firing cycle
		fireCycle++;
		if (fireCycle > 5){
			fireCycle = 0;
		}
	}

	public void FireFaster () {
		if (fireMode < 2) {
			SetFireMode(fireMode + 1);
			PowerUp();
		}
		//else
		//	PowerUpBoom();
	}

	public void SetFireMode (int mode) {
		if (mode == 1) {
			fireMode = 1;
			audioCycleMin = 0;
			audioCycleMax = audioArray.Length / 2;	// Should still work for Length=1, since cycle will always reset to 0
			audioCycle = audioCycleMin;
		}
		else if (mode == 2) {
			fireMode = 2;
			audioCycleMin = audioArray.Length / 2;
			audioCycleMax = audioArray.Length;
			audioCycle = audioCycleMin;
		}
	}
	
	void PowerUp () {
		//Destroy(Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		GameObject powerUpEffect = Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
		powerUpEffect.transform.parent = transform;
		Destroy(powerUpEffect, 1.5f);
	}
	
	public void BombMinusTwo () {
		//Destroy(Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)), 1.5f);
		if (bombMinusTwo) {
			GameObject powerUpEffect = Instantiate(bombMinusTwo, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
			powerUpEffect.transform.parent = transform;
			Destroy(powerUpEffect, 1.5f);
		}
	}

	public void BombMinusOne () {
		//Destroy(Instantiate(powerUp, transform.position, Quaternion.Euler(0, 0, 0)), 1.5f);
		if (bombMinusOne) {
			GameObject powerUpEffect = Instantiate(bombMinusOne, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
			powerUpEffect.transform.parent = transform;
			Destroy(powerUpEffect, 1.5f);
		}
	}

	public void GiveBomb (bool forced) {
		hasBomb = true;
		if (!forced) {
			// Powerup effect (transitory)
			if (bombAcquired) {
				GameObject powerUpEffect = Instantiate(bombAcquired, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
				powerUpEffect.transform.parent = transform;
				Destroy(powerUpEffect, 1.5f);
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
		
		/* New code
		// Kill within inner radius
		List<GameObject> enemies = groundControl.FindAllWithinRadius(transform.position, bombKillRadius, groundControl.Extent3D);
		foreach (GameObject enemy in enemies) {
			enemy.SendMessage("Die", false);
		}

		// Push within outer radius
		enemies = groundControl.FindAllWithinRadius(transform.position, bombPushRadius, groundControl.Extent3D);
		foreach (GameObject enemy in enemies) {
			enemy.GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
		}
		*/

		// Old code
		Collider[] enemies;
		int mask = 1 << enemyLayer;
		
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
		//
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
