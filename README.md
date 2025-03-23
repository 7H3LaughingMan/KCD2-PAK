# KCD2 PAK

A rather simple application that is designed to help create the necessary `.pak` files to assist with creating mods for Kingdom Come Deliverance 2. This works by dragging your mod folder/s onto the application itself. It works by doing the following.

1. It checks that you have a `mod.manifest` in the folder and contains a `<modid></modid>`.
2. It packages everything in `.\Data\*` , except for `.\Data\Levels\*`, into `.\Data\modid.pak`.
3. It packages everything in `.\Localization\**\*` into `.\Localization\**.pak`.
4. It packages everything in `.\Data\Levels\**\*` into `.\Data\Levels\**\modid.pak`

## Examples

Let's say you have a mod with `<modid>example</modid>`

Any data modifications would go into `.\Data\` folder and will be packed into `.\Data\example.pak`

Any English text localizations will go into the `.\Data\Localization\English_xml\` folder and will be packed into `.\Data\Localization\English_xml.pak`.

Any Trosky level modifications would go into the `.\Data\Levels\trosecko\` folder and will be packed into `.\Data\Levels\trosecko\example.pak`.
