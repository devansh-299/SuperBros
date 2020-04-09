using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {
	
	// enemy move speed
	[Range(0f, 15f)]	// gives slider in the editor
	public float moveSpeed = 4f;

	// damage given to the player
	public int damageAmount = 10;

	// gameobject used to check stunned state of enemy
	[Tooltip("Child game object used for detecting stun")]	// tooltip provided in inspector
	public GameObject stunnedCheck; 

	// stunned time
	[HideInInspector]	// for hiding this public field from Inspector
	public float stunnedTime = 3f;   
	
	// layer to put enemy on when stunned
	public string stunnedLayer = "StunnedEnemy"; 

	// the player layer 
	public string playerLayer = "Player";  
	
	public bool isStunned = false;
	
	// List of Waypoints for enemy's movements
	public GameObject[] myWaypoints; 
	
	// time to wait at waypoint
	public float waitAtWaypointTime = 1f;
	
	public bool loopWaypoints = true;
	
	// SFXs
	public AudioClip stunnedSFX;
	public AudioClip attackSFX;
	
	
	// store references to components on the gameObject
	// underscore is used here to denote private variables
	Transform _transform;
	Rigidbody2D _rigidbody;
	Animator _animator;
	AudioSource _audio;
	
	// used to show Private fields in Inspector
	[SerializeField]
	int _myWaypointIndex = 0; // used as index for My_Waypoints
	float _moveTime; 
	float _vx = 0f;
	bool _moving = true;
	
	// store the layer number the enemy is on (setup in Awake)
	int _enemyLayer;

	// store the layer number the enemy should be moved to when stunned
	int _stunnedLayer;
	
	void Awake() {

		// get a reference to the components for efficiency purposes
		_transform = GetComponent<Transform> ();
		
		_rigidbody = GetComponent<Rigidbody2D> ();
		// if Rigidbody is missing
		if (_rigidbody == null) 
			Debug.LogError("Rigidbody2D component missing from this gameobject");
		
		_animator = GetComponent<Animator>();
		// if Animator is missing
		if (_animator == null) 
			Debug.LogError("Animator component missing from this gameobject");
		
		_audio = GetComponent<AudioSource> ();
		// if AudioSource is missing
		if (_audio == null) { 
			Debug.LogWarning("AudioSource component missing from this gameobject. Adding one.");
			// AudioSource component created dynamically
			_audio = gameObject.AddComponent<AudioSource>();
		}

		// if stunnedCheck gameObject is missing
		if (stunnedCheck==null) {
			Debug.LogError("stunnedCheck child gameobject needs to be setup on the enemy");
		}
		
		// setup moving defaults
		_moveTime = 0f;
		_moving = true;
		
		// determine the enemies specified layer
		_enemyLayer = this.gameObject.layer;

		// determine the stunned enemy layer number
		// here stunnedLayer is name of the name of the layer
		_stunnedLayer = LayerMask.NameToLayer(stunnedLayer);

		// ignore collisions between the playerLayer and the stunnedLayer
		Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(playerLayer), _stunnedLayer, true); 
	}
	
	// if not stunned then move the enemy when time is > _moveTime
	void Update () {

		if (!isStunned) {
			// Time.time gives the elapsed time (sec) since the game started
			if (Time.time >= _moveTime) {
				EnemyMovement();
			} else {
				_animator.SetBool("Moving", false);
			}
		}
	}
	
	// Move the enemy through its rigidbody based on its waypoints
	void EnemyMovement() {

		// if waypoint list is not empty
		if ((myWaypoints.Length != 0) && (_moving)) {
			
			Flip (_vx);
			
			// determine distance between waypoint and enemy
			_vx = myWaypoints[_myWaypointIndex].transform.position.x-_transform.position.x;
			
			// if the enemy is close enough to waypoint, make it's new target the next waypoint
			if (Mathf.Abs(_vx) <= 0.05f) {

				// At waypoint so stop moving
				_rigidbody.velocity = new Vector2(0, 0);
				
				// increment to next index in array
				_myWaypointIndex++;
				
				// reset waypoint back to 0 for looping
				if(_myWaypointIndex >= myWaypoints.Length) {
					if (loopWaypoints)
						_myWaypointIndex = 0;
					else
						_moving = false;
				}
				
				// setup wait time at current waypoint
				_moveTime = Time.time + waitAtWaypointTime;
			} else {
				// enemy is moving
				_animator.SetBool("Moving", true);
				
				// Set the enemy's velocity to moveSpeed in the x direction.
				_rigidbody.velocity = new Vector2(_transform.localScale.x * moveSpeed, _rigidbody.velocity.y);
			}
			
		}
	}
	
	// flip the enemy to face torward the direction he is moving in
	void Flip(float _vx) {
		
		// get the current scale
		Vector3 localScale = _transform.localScale;
		
		if ((_vx>0f)&&(localScale.x<0f))
			localScale.x*=-1;
		else if ((_vx<0f)&&(localScale.x>0f))
			localScale.x*=-1;
		
		// update the scale
		_transform.localScale = localScale;
	}
	
	// for attacking the player
	void OnTriggerEnter2D(Collider2D collision) {

		if ((collision.tag == "Player") && !isStunned) {

			CharacterController2D playerScript = collision.gameObject.GetComponent<CharacterController2D>();
			if (playerScript.playerCanMove) {

				// Make sure the enemy is facing the player on attack
				Flip(collision.transform.position.x - _transform.position.x);
				
				// attack sound
				playSound(attackSFX);
				
				// stop moving
				_rigidbody.velocity = new Vector2(0, 0);
				
				// apply damage to the player
				playerScript.ApplyDamage (damageAmount);
				
				// stop enemy to attack the player
				_moveTime = Time.time + stunnedTime;
			}
		}
	}
	
	// if the Enemy collides with a MovingPlatform, then make it a child of that platform
	// so it will go for a ride on the MovingPlatform
	void OnCollisionEnter2D(Collision2D other) {

		if (other.gameObject.tag=="MovingPlatform") {
			this.transform.parent = other.transform;
		}
	}
	
	// if the enemy exits a collision with a moving platform, then unchild it
	void OnCollisionExit2D(Collision2D other) {
		if (other.gameObject.tag=="MovingPlatform") {
			this.transform.parent = null;
		}
	}
	
	// play sound through the audiosource on the gameobject
	void playSound(AudioClip clip) {
		_audio.PlayOneShot(clip);
	}
	
	// setup the enemy to be stunned
	public void Stunned() {

		if (!isStunned) {

			isStunned = true;
			
			// provide the player with feedback that enemy is stunned
			playSound(stunnedSFX);
			_animator.SetTrigger("Stunned");
			
			// stop moving
			_rigidbody.velocity = new Vector2(0, 0);
			
			// switch layer to stunned layer so no collisions with the player while stunned
			this.gameObject.layer = _stunnedLayer;
			stunnedCheck.layer = _stunnedLayer;

			// start coroutine to stand up eventually
			StartCoroutine (Stand ());
		}
	}
	
	// coroutine to unstun the enemy and stand back up
	IEnumerator Stand() {
		yield return new WaitForSeconds(stunnedTime); 
		
		// no longer stunned
		isStunned = false;
		
		// switch layer back to regular layer for regular collisions with the player
		this.gameObject.layer = _enemyLayer;
		stunnedCheck.layer = _enemyLayer;
		
		// provide the player with feedback
		_animator.SetTrigger("Stand");
	}
}
