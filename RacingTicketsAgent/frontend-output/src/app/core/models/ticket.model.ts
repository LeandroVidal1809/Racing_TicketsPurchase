export type EstadoTicket = 'confirmado' | 'pendiente' | 'cancelado';

export interface Ticket {
  id: string;
  partidoId: string;
  sectorId: string;
  sectorNombre: string;
  partidoRival: string;
  partidoFecha: Date;
  compradorNombre: string;
  compradorDni: string;
  compradorEmail: string;
  precio: number;
  fechaCompra: Date;
  qrCode: string;
  estado: EstadoTicket;
}

export interface OrdenCompra {
  partidoId: string;
  sectorId: string;
  cantidad: number;
  compradorNombre: string;
  compradorDni: string;
  compradorEmail: string;
}
