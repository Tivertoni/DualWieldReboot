# Dual Wield - Reboot

Thanks to jedijosh920 for the <a href="https://www.gta5-mods.com/scripts/dual-wield">original mod</a>

Please credit him if you use this code for your project

A wise man used to say "Double the gun, Double the fun". That's why Dual Wield mod by Jedijosh is all we need, sadly it's outdated.   
I've decided to redo the script and make it slightly prettier with a few new features. 

Features:
<ul type="square">
<li>Ammo is now independent, it takes the original ammo and doubles it. It has its own ammo counter (on top right corner)</li>
<li>No more frequent crashes (e.g entering vehicle used to crash the mod)</li>
<li>More damage, now it adds player's damage. It used to utilize NPC damage only</li>
<li>Now, the reload will be done twice and weapons are less accurate. Duh! you are holding two guns</li>
<li>Fire rate is the same, cause it's the same gun only it holds and spits more bullets</li>
<li>Changing weapons while dual-wielding with no crashes. Heavy guns and explosives are now forbidden to be dual-wielded</li>
<li>Attachments on the firearm will be carried over to the dual-wielded one</li>
<li>Controller supported, aim and press Phone Right (DPad right on default game setting)</li>
<li>Improved animations from the original one, without using custom anims</li></ul>
Requirement:
ScriptHookVDotNet3, I strongly recommend having the latest <a href="https://github.com/scripthookvdotnet/scripthookvdotnet-nightly">Nightly Version</a>
This script was made on game version 3179, other versions are untested.

Bugs: 
<ul>
<li>First Person sucks, weapons won't accurately follow crosshair, but it still shoots on the target, it's just the animation</li>
<li>Aiming from one object to the other with a big distance gap (e.g close target vs the sky) makes hands a little bit jumpy</li>
<li>Please reload in safe place, whenever you reload the character will stop</ul><b>Incompatibility</b>: 
<b>More Gore</b> mod by IAmJFry.  If you have this, disable its PlayerHealingAnimation in its XML settings, otherwise you'll have two overlapping guns in your right hand
<b>Addon Weapons</b> are untested, i never used addon weapon mods, so I can't guarantee this will work with those addon guns

Changelog:
1.1
<ul type="disc">
<li>Dual Wield now works on Shootdodge v1.3 with limitation (Can't aim 360°, aim forward only)</li>
<li>Animation is adjusted, now the guns on FPS view don't look too high</li>
<li>Added animations when aiming very high or very low (e.g aiming heli or enemy below you), if it looks weird for you, you can turn it off in the config</li><li>Animations altered when entering cover or jumping (the hands won't be closed together when holding rifles/smg)</li>
<li>Added recoils to the camera (a.k.a fake recoil), adjustable in the config</li>
<li>Player damage is raised, especially for non-auto guns</ul>1.0.
<ul type="disc">
<li>First release</li></ul>
