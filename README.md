<h1 align="center">IBM SkillsBuild VR</h1>

<p align="center">
  <strong>Immersive Active Learning Prototype</strong><br>
  <em>A cutting edge software engineering prototype for IBM</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Engine%2DUnity_XR%2Dblueviolet" alt="Engine" />
  <img src="https://img.shields.io/badge/AI%2DLLaMA_3.2%2Dorange" alt="AI" />
  <img src="https://img.shields.io/badge/Hardware%2DMeta_Quest_3%2Dlightgrey" alt="Hardware" />
  <img src="https://img.shields.io/badge/Platform%2DPC_VR%2Dsuccess" alt="Platform" />
  <img src="https://img.shields.io/badge/License%2DMIT%2Dgreen" alt="License" />
</p>

<p align="center">
  <a href="#quick_start">Quick Start</a> &bull;
  <a href="#what_you_get">Features</a> &bull;
  <a href="#architecture">Architecture</a> &bull;
  <a href="#usability">UX Design</a> &bull;
  <a href="#installation">Setup</a> &bull;
  <a href="#troubleshooting">Troubleshooting</a>
</p>

<h2 id="in_action">IBM SkillsBuild VR in action</h2>

<p align="center">
  <img src="./docs/image1.jpg" alt="Overview of the virtual environment" width="800" />
  <br>
  <em>The Fantasy World Hub displaying the distinct zones</em>
</p>

<p align="center">
  <img src="./docs/image2.png" alt="Virtual robot assistant" width="800" />
  <br>
  <em>The Granite AI Companion guiding the player</em>
</p>

<hr>

<h2 id="what_you_get">What You Get</h2>

<ul>
  <li>Drop in VR educational experience built on OpenXR for Meta Quest and PC VR.</li>
  <li>Locally hosted Llama 3.2 Large Language Model via LLMUnity running completely offline.</li>
  <li>Advanced Text to Speech generation via Piper TTS ensuring zero latency voice delivery.</li>
  <li>Four distinct fantasy biomes: Forest (AI Ethics), Volcano (Building Agents), Castle (AI Agents), and Maze (Text to Speech).</li>
  <li>Physical ingredient collection quiz mechanics avoiding traditional immersion breaking UI panels.</li>
  <li>Context aware AI tutoring that dynamically generates hints based on specific player mistakes.</li>
  <li>Event driven decoupled Unity architecture designed for scalability and multiplayer expansion.</li>
</ul>

<h2 id="introduction">1. Introduction & Project Summary</h2>

<p>This project is an immersive educational VR game built in Unity that combines interactive quiz based learning with AI powered tutoring within a thematic fantasy world. The project was motivated by the challenge of making complex topics in AI ethics and text to speech technology accessible and engaging for university learners. Research consistently demonstrates that immersive VR environments improve motivation, knowledge retention, and learning outcomes over conventional methods, with experiential and constructivist learning identified as the theoretical foundations most suited to VR based instruction. Gamification further strengthens engagement: game mechanics such as immediate feedback, challenge progression, and rewards measurably increase soft skill development in e learning contexts.</p>

<p>The player begins in the Main Hub, where the Granite AI Companion, a robot assistant powered by a locally hosted large language model via LLMUnity and LlamaLib, greets them and introduces the experience. From the hub, the player travels via hoverboard to the Fantasy world, divided into four distinct zones each presenting a quiz on a different AI concept. Each zone features a personality driven NPC guide and a physical ingredient collection quiz mechanic, in which the player selects objects corresponding to their chosen answers displayed in a quiz book and submits them at a central cauldron.</p>

<p>The client is IBM, who commissioned the tool to teach learners about key AI concepts through the IBM SkillsBuild platform. The primary goals are to deliver an engaging gamified learning experience that improves retention over traditional training, and to integrate real time AI powered personalised feedback that adapts to each learner’s mistakes.</p>

<h2 id="architecture">2. Technical Architecture</h2>

<h3>2.1 Source Materials</h3>
<p>The following libraries, tools, and resources formed the technical foundation of the project:</p>
<ul>
  <li><b>Game Engine:</b> Unity 6000.3.3f1 served as the primary development environment.</li>
  <li><b>LLM Core:</b> Meta’s Llama 3.2 1B or 3B Instruct model, quantized to GGUF format for local on device inference with a reduced memory footprint.</li>
  <li><b>LLM Integration:</b> The LLMUnity package with LlamaLib v2.0.2 for running the language model directly within Unity via bundled native libraries.</li>
  <li><b>Text to Speech:</b> Piper TTS using ONNX based English voice models for low latency, offline voice output bundled in StreamingAssets.</li>
  <li><b>VR Framework:</b> OpenXR 1.16.1 and XR Interaction Toolkit 3.3.0 for platform agnostic headset support.</li>
  <li><b>Input Handling:</b> Unity Input System 1.17.0 for event driven input supporting both VR controllers and keyboard/mouse from a single action map.</li>
  <li><b>Navigation:</b> Unity AI Navigation package for NavMesh baking and NavMeshAgent based pathfinding used by the Granite AI Companion robot.</li>
  <li><b>Text Rendering:</b> TextMeshPro with SDF (Signed Distance Field) fonts for sharp text rendering at variable distances in VR.</li>
  <li><b>Player Movement:</b> Unity’s CharacterController rather than Rigidbody based movement, chosen to avoid physics based drift and collision jitter.</li>
  <li><b>3D Assets:</b> Environment models created in Blender by the design team, exported as FBX and imported into Unity with URP compatible materials.</li>
</ul>

<h3>2.2 High Level System Architecture</h3>
<p>The project follows a layered, event driven architecture. Scripts are organised into a Core layer that runs in every scene, a World layer containing mechanics specific to each themed world, and an Interface layer that connects the two without tight coupling.</p>

<p><b>Core Layer (always active)</b></p>
<ul>
  <li><code>GameState</code>: Tracks the player’s current mode (Hoverboard or Station) and fires an OnStateChanged event whenever the mode changes.</li>
  <li><code>GameSettings</code>: Detects whether a VR headset is connected at startup and exposes a static boolean. Every script with platform specific behaviour branches on this single flag.</li>
  <li><code>PlayerMovementCC</code>: CharacterController based movement script that reads input from the Input System’s Move action.</li>
  <li><code>PlayerGrab</code>: Universal grab system handling picking up, holding, releasing, and throwing objects. On KBM, grabbing uses a raycast. On VR, it uses an OverlapSphere check.</li>
  <li><code>RobotAI & RobotVisual</code>: Implements the robot companion using a two GameObject architecture to separate logical pathfinding from floating visual interpolation.</li>
  <li><code>RobotInteract</code>: Detects when the player wants to talk to the robot using an angle and distance check.</li>
</ul>

<p><b>World Layer (fantasy specific)</b></p>
<ul>
  <li><code>RobotAbsorb</code>: Handles the ingredient to robot delivery mechanic. The absorption animation plays a three phase sequence: magnetic pull, brief hover, then shrink into the robot.</li>
  <li><code>IngredientTracker</code>: Stores the player’s ingredient choice per station. Evaluates results only at the cauldron after all stations are visited, creating a deliberate delayed feedback loop.</li>
  <li><code>FantasyCauldron</code>: Handles the brew sequence. When the player has collected an ingredient from every station and interacts with the cauldron, all orbiting objects fly off the robot into the cauldron one by one.</li>
  <li><code>FantasyRobotBrain</code>: Implements the IRobotBrain interface and provides context aware dialogue when the player interacts with the robot.</li>
</ul>

<h3>2.3 Design Principles and Patterns</h3>
<ul>
  <li><b>Singleton Pattern:</b> Used for IngredientTracker, IntroManager, and FantasyWorldManager to avoid passing references through long chains of serialised fields.</li>
  <li><b>Observer Pattern:</b> Static C# events decouple producers from consumers. Adding a new visual effect only requires subscribing to the event.</li>
  <li><b>Strategy Pattern:</b> The IRobotBrain interface allows each world to provide its own dialogue logic. RobotInteract holds a reference of type IRobotBrain and calls it without knowing the concrete type.</li>
  <li><b>Delegate Registration:</b> PlayerGrab uses a delegate to inject world specific click behaviour, avoiding subclassing for each world.</li>
  <li><b>ScriptableObject Data:</b> Game data is stored in ScriptableObject assets defining IDs, display names, orb colours, and prefabs. Changes propagate everywhere without code changes.</li>
  <li><b>Coroutine State Machine:</b> RobotIntroController manages the tutorial sequence using an enum and chained coroutines, adapting to player driven transitions.</li>
</ul>

<h2 id="usability">3. Usability and User Experience</h2>
<p>Usability in VR presents distinct challenges compared to conventional applications: screen space UI elements cause discomfort when fixed to the player’s view, and interaction paradigms from desktop or mobile contexts do not translate naturally to head mounted displays. The design decisions below address these challenges directly.</p>

<ul>
  <li><b>Diegetic UI over HUD elements:</b> All UI in this project is world space: speech bubbles float near the robot, interaction prompts orbit the robot facing the player, quiz books are physical objects, and progress is shown via orbiting orbs. This approach aligns with research identifying immersive, embodied interaction as central to effective VR learning environments.</li>
  <li><b>Platform adaptive interaction prompts:</b> Prompts display the correct button icon for the current platform driven by a single InputIconMap ScriptableObject.</li>
  <li><b>Drip feed tutorial:</b> The introduction teaches one mechanic at a time, waiting for the player to demonstrate understanding before progressing.</li>
  <li><b>Forgiving interaction detection:</b> Detection was redesigned to use an angle and distance check. If the player is within range and looking roughly toward the robot (within a 30 degree cone), the interaction registers effortlessly.</li>
  <li><b>VR text readability:</b> All text uses TextMeshPro with SDF (Signed Distance Field) fonts, which remain sharp at variable viewing distances.</li>
  <li><b>Smooth transitions:</b> Station entry and exit uses a fade to black transition rendered on a sphere surrounding the player camera, maintaining stereoscopic depth perception.</li>
</ul>

<h2 id="quick_start">4. System Access and Setup</h2>

<h3>Requirements</h3>
<ul>
  <li>Windows 10+ or macOS 12+</li>
  <li>Unity Editor version 6000.3.3f1</li>
  <li>Meta Quest 2 or 3 headset (optional, application also runs in desktop mode)</li>
  <li>Minimum specification: quad core 2.5 GHz CPU, 8GB RAM, dedicated GPU with 4GB+ VRAM.</li>
</ul>

<h3>Setup Steps</h3>
<ol>
  <li>Unzip the downloaded project to a local directory.</li>
  <li>Open Unity Hub, select Open, Add project from disk, and navigate to the project folder.</li>
  <li>Open the project using Unity Editor version 6000.3.3f1. Unity will automatically resolve all Package Manager dependencies on first open.</li>
  <li>In the Project window, navigate to <code>Assets/_Project/Scenes/</code> and open the scene <code>MainHub</code>.</li>
  <li>Verify that the LLM model file (.gguf) is present in <code>StreamingAssets/LlamaLib_v2.0.2/</code>. If missing, open the LLM GameObject inspector and use the Download Model option.</li>
</ol>

<p><b>Running in Desktop Mode:</b> Press Play in the Unity Editor. The application detects that no VR headset is connected and launches automatically in desktop mode, controllable via keyboard and mouse.</p>

<p><b>Running in VR Mode:</b> Enable developer mode on the Meta Quest headset. Connect the headset via USB using Quest Link, or enable Air Link wirelessly. In Unity, navigate to Project Settings, XR Plug in Management and ensure OpenXR is enabled for the PC platform. Press Play in the Unity Editor to launch directly on the connected headset.</p>

<h2 id="requirements">5. Behavioural Requirements Status</h2>

<p>The following highlights the implementation status of key behavioural requirements.</p>

<table>
  <tr>
    <th>Category</th>
    <th>Implementation Details</th>
  </tr>
  <tr>
    <td><b>Character Spawn</b></td>
    <td>Player spawns at the hub centre based on the XR Origin prefab position. MouseLook provides full 360 degree horizontal and vertical rotation on desktop; VR head tracking provides unrestricted rotation natively.</td>
  </tr>
  <tr>
    <td><b>Portal Transitions</b></td>
    <td>Portal transitions are implemented via PortalDoor using SceneManager for hub to world transitions. Intra world station transitions use local repositions with a fade to black mechanism.</td>
  </tr>
  <tr>
    <td><b>Skill Quizzes</b></td>
    <td>Answer bridges were replaced with a physical book and ingredient system. Ingredient selection is recorded and evaluated only at the cauldron after all stations are visited, creating a deliberate delayed feedback loop.</td>
  </tr>
  <tr>
    <td><b>Adaptive Hints</b></td>
    <td>Tiered hint system delivers vague, proximity, and direct hints based on cauldron failure count. Hints are spoken aloud via Piper TTS. Proximity based object interaction enables help requests.</td>
  </tr>
</table>

<h2 id="troubleshooting">6. Troubleshooting</h2>

<table>
  <tr>
    <th>Symptom</th>
    <th>Likely Cause & Solution</th>
  </tr>
  <tr>
    <td><b>Model file not found</b><br>(llama3.2_3b_instruct_q4_k_m.gguf)</td>
    <td>LLM model missing from project. Open LLM inspector, click "Load model" or "Download model". Point to <code>StreamingAssets/LlamaLib_v2.0.2/</code>.</td>
  </tr>
  <tr>
    <td><b>LLM failed to start</b></td>
    <td>Model missing or insufficient RAM. Ensure you have 8GB+ RAM. Disable the LLM GameObject to suppress errors and fallback to template lines.</td>
  </tr>
  <tr>
    <td><b>Robot does not move during intro</b></td>
    <td>NavMesh not baked. Go to Window > AI > Navigation, then Bake. Verify PathFollower Y position.</td>
  </tr>
  <tr>
    <td><b>Grabbed objects ignore clicks</b></td>
    <td>Set each ingredient’s Layer to "Grabbable" in the inspector. Verify PlayerGrab’s layer mask includes it.</td>
  </tr>
  <tr>
    <td><b>Unity editor crashes on Play</b></td>
    <td>LLM model loading exceeds available RAM. Disable LLM GameObject and close other heavy applications. Check RAM in Task Manager.</td>
  </tr>
  <tr>
    <td><b>NullReferenceException on Cauldron</b></td>
    <td>Inspector fields unassigned. Script includes null guards. Ensure fields are assigned when active. In non fantasy scenes, leave component disabled.</td>
  </tr>
  <tr>
    <td><b>Portal door opens from wrong location</b></td>
    <td>doorTriggerPoint reference not assigned or incorrectly positioned. Create empty GameObject at doorway position and assign it.</td>
  </tr>
</table>

<h2 id="maintenance">7. Maintenance and Implications</h2>

<h3>7.1 System Maintenance</h3>
<ul>
  <li><b>Version Control:</b> The project uses Unity Version Control (Plastic SCM), integrated into the Unity editor. Ignore rules exclude Unity generated folders (Library, Temp, Logs).</li>
  <li><b>Dependencies:</b> Managed through Packages/manifest.json, including XR Interaction Toolkit, OpenXR, Input System, URP, and AI Navigation. Git based dependencies must be updated manually.</li>
  <li><b>Performance:</b> Unity’s built in Profiler and Frame Debugger are used to monitor draw calls, frame time, and physics overhead which is critical for stable VR frame rates.</li>
</ul>

<h3>7.2 Future Development</h3>
<ul>
  <li><b>Offline TTS Fallback:</b> Integrating PiperSpeakerComponent as the primary fallback when external TTS is unavailable to provide reliable offline voices.</li>
  <li><b>VR Voice Input:</b> Extending the VoiceManager to VR controller button mapping to enable native speech to text dialogue with the robot.</li>
  <li><b>Physical Movement Handling:</b> Re enabling the CounterPhysicalMovement method to prevent VR players from walking through walls during room scale play.</li>
  <li><b>Multiplayer Support:</b> The architecture cleanly separates player, robot, and station components, making multiplayer expansion highly feasible via Unity’s Netcode for GameObjects.</li>
  <li><b>Persistent Progress System:</b> Using JSON based save files for quiz progress, amulets, and world completion to allow players to resume their progress seamlessly across different VR sessions.</li>
</ul>

<h2 id="team">8. The Team</h2>

<p>
  <b>Erica Da Silva</b> &bull; <b>Renan Sho Marie</b> &bull; <b>Petru Iustin</b> &bull; <b>Ruixi Yang</b> &bull; <b>Palak Shah</b><br>
  Group 27
</p>

<p align="center">
  <em>Developed as a software engineering prototype for COMP2201 (2025/2026). For any inquiries or contributions, please open an issue or submit a pull request.</em>
</p>
