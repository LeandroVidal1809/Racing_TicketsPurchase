export type Torneo = 'Liga Profesional' | 'Copa Argentina' | 'Copa Libertadores' | 'Amistoso';

export type DisponibilidadEstado = 'available' | 'few' | 'soldout';

export interface Sector {
  id: string;
  nombre: string;
  precio: number;
  capacidad: number;
  disponibles: number;
  descripcion?: string;
}

export interface Partido {
  id: string;
  rival: string;
  rivalEscudo?: string;
  fecha: Date;
  torneo: Torneo;
  estadio: string;
  esLocal: boolean;
  sectores: Sector[];
  imagen?: string;
  destacado?: boolean;
}

// ── Helpers ───────────────────────────────────────────────────────────────────

export function getDisponibilidad(sector: Sector): DisponibilidadEstado {
  const pct = sector.disponibles / sector.capacidad;
  if (sector.disponibles === 0) return 'soldout';
  if (pct < 0.15)               return 'few';
  return 'available';
}

export function getPrecioMinimo(partido: Partido): number {
  return Math.min(...partido.sectores.map(s => s.precio));
}

export function getDisponibilidadLabel(estado: DisponibilidadEstado): string {
  const labels: Record<DisponibilidadEstado, string> = {
    available: 'Disponible',
    few:       'Pocas entradas',
    soldout:   'Agotado'
  };
  return labels[estado];
}
