float4 VS(float2 position : POSITION) : SV_POSITION
{
    return float4(position, 0.0, 1.0); // Преобразуем 2D координаты в 4D
}