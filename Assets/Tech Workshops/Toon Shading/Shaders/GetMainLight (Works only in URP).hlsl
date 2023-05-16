//Checks if GETMAINLIGHT_INCLUDED is not defined, if not, it runs the custom function.
//This is important for every custom function used in Shadergraph. Otherwise the custom function gets duplicated by each shader using it
#ifndef GETMAINLIGHT_INCLUDED
#define GETMAINLIGHT_INCLUDED

//The custom function MUST append the _precision suffix, whether it is _float or _half. This is an optimization pass that depends of the floating point precision chosen for the shader.
//When referencing the name of the custom function in Shadergraph, DO NOT reference the suffix.
//For instance, here the name of the custom function is GetMainLight and not GetMainLight_float
void GetMainLight_float(out float3 Direction, out float3 Color)
{
#ifdef SHADERGRAPH_PREVIEW
	Direction = float3(0.5, 0.5, 0);
	Color = 1;
#else
    Light light = GetMainLight();
    Direction = light.direction;
    Color = light.color;
#endif
}
#endif //Ends the GETMAINLIGHT_INCLUDED condition check