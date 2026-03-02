import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';
import { Partido, Ticket, OrdenCompra } from '../models/partido.model';
import { Ticket as TicketModel } from '../models/ticket.model';

@Injectable({ providedIn: 'root' })
export class PartidosService {

  // ── Mock Data ───────────────────────────────────────────────────────────────
  private readonly mockPartidos: Partido[] = [
    {
      id: '1',
      rival: 'Boca Juniors',
      fecha: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000),
      torneo: 'Liga Profesional',
      estadio: 'El Cilindro',
      esLocal: true,
      destacado: true,
      sectores: [
        { id: 's1', nombre: 'Popular Norte', precio: 8000,  capacidad: 15000, disponibles: 1200 },
        { id: 's2', nombre: 'Platea Baja',   precio: 15000, capacidad: 8000,  disponibles: 420  },
        { id: 's3', nombre: 'Palcos',         precio: 35000, capacidad: 1200,  disponibles: 80   }
      ]
    },
    {
      id: '2',
      rival: 'River Plate',
      fecha: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000),
      torneo: 'Liga Profesional',
      estadio: 'El Cilindro',
      esLocal: true,
      sectores: [
        { id: 's1', nombre: 'Popular Norte', precio: 9000,  capacidad: 15000, disponibles: 3200 },
        { id: 's2', nombre: 'Platea Baja',   precio: 18000, capacidad: 8000,  disponibles: 2100 },
        { id: 's3', nombre: 'Palcos',         precio: 40000, capacidad: 1200,  disponibles: 350  }
      ]
    },
    {
      id: '3',
      rival: 'Independiente',
      fecha: new Date(Date.now() + 21 * 24 * 60 * 60 * 1000),
      torneo: 'Copa Argentina',
      estadio: 'El Cilindro',
      esLocal: true,
      sectores: [
        { id: 's1', nombre: 'Popular Norte', precio: 7000,  capacidad: 15000, disponibles: 0    },
        { id: 's2', nombre: 'Platea Baja',   precio: 13000, capacidad: 8000,  disponibles: 150  },
        { id: 's3', nombre: 'Palcos',         precio: 30000, capacidad: 1200,  disponibles: 200  }
      ]
    },
    {
      id: '4',
      rival: 'Flamengo',
      fecha: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
      torneo: 'Copa Libertadores',
      estadio: 'El Cilindro',
      esLocal: true,
      sectores: [
        { id: 's1', nombre: 'Popular Norte', precio: 12000, capacidad: 15000, disponibles: 5000 },
        { id: 's2', nombre: 'Platea Baja',   precio: 22000, capacidad: 8000,  disponibles: 3200 },
        { id: 's3', nombre: 'Palcos',         precio: 55000, capacidad: 1200,  disponibles: 600  }
      ]
    }
  ];

  private mockTickets: TicketModel[] = [];

  // ── Public API ──────────────────────────────────────────────────────────────

  getPartidos(): Observable<Partido[]> {
    return of(this.mockPartidos).pipe(delay(400));
  }

  getPartidoById(id: string): Observable<Partido | undefined> {
    return of(this.mockPartidos.find(p => p.id === id)).pipe(delay(300));
  }

  getPartidoDestacado(): Observable<Partido | undefined> {
    return of(this.mockPartidos.find(p => p.destacado) ?? this.mockPartidos[0]).pipe(delay(200));
  }

  getProximosPartidos(limit = 3): Observable<Partido[]> {
    return of(this.mockPartidos.slice(0, limit)).pipe(delay(300));
  }

  comprarTicket(orden: OrdenCompra): Observable<TicketModel> {
    const partido = this.mockPartidos.find(p => p.id === orden.partidoId)!;
    const sector  = partido.sectores.find(s => s.id === orden.sectorId)!;

    const ticket: TicketModel = {
      id:              `TKT-${Date.now()}`,
      partidoId:       orden.partidoId,
      sectorId:        orden.sectorId,
      sectorNombre:    sector.nombre,
      partidoRival:    partido.rival,
      partidoFecha:    partido.fecha,
      compradorNombre: orden.compradorNombre,
      compradorDni:    orden.compradorDni,
      compradorEmail:  orden.compradorEmail,
      precio:          sector.precio * orden.cantidad,
      fechaCompra:     new Date(),
      qrCode:          `QR-${Math.random().toString(36).substring(2, 10).toUpperCase()}`,
      estado:          'confirmado'
    };

    this.mockTickets.push(ticket);
    return of(ticket).pipe(delay(800));
  }

  getMisTickets(): Observable<TicketModel[]> {
    return of(this.mockTickets).pipe(delay(300));
  }
}
