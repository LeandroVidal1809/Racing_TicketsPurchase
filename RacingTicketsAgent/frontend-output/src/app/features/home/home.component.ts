import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PartidosService } from '../../core/services/partidos.service';
import { Partido, getPrecioMinimo, getDisponibilidad, getDisponibilidadLabel } from '../../core/models/partido.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {

  partidoDestacado?: Partido;
  proximosPartidos: Partido[] = [];
  loading = true;

  // Countdown
  countdown = { dias: 0, horas: 0, minutos: 0, segundos: 0 };
  private countdownInterval?: ReturnType<typeof setInterval>;

  // Expose helpers to template
  getPrecioMinimo = getPrecioMinimo;
  getDisponibilidad = getDisponibilidad;
  getDisponibilidadLabel = getDisponibilidadLabel;

  constructor(private partidosService: PartidosService) {}

  ngOnInit(): void {
    this.partidosService.getPartidoDestacado().subscribe(partido => {
      this.partidoDestacado = partido;
      if (partido) this.startCountdown(partido.fecha);
    });

    this.partidosService.getProximosPartidos(4).subscribe(partidos => {
      this.proximosPartidos = partidos;
      this.loading = false;
    });
  }

  ngOnDestroy(): void {
    if (this.countdownInterval) clearInterval(this.countdownInterval);
  }

  private startCountdown(fecha: Date): void {
    const update = () => {
      const diff = fecha.getTime() - Date.now();
      if (diff <= 0) { clearInterval(this.countdownInterval); return; }
      this.countdown = {
        dias:     Math.floor(diff / 86400000),
        horas:    Math.floor((diff % 86400000) / 3600000),
        minutos:  Math.floor((diff % 3600000) / 60000),
        segundos: Math.floor((diff % 60000) / 1000)
      };
    };
    update();
    this.countdownInterval = setInterval(update, 1000);
  }

  pad(n: number): string {
    return n.toString().padStart(2, '0');
  }

  trackById(_: number, p: Partido): string { return p.id; }

  whyItems = [
    { icon: '🏟️', title: 'El Cilindro',     desc: 'Viví la mística del estadio más emblemático de Avellaneda con capacidad para 60.000 personas.' },
    { icon: '🎉', title: 'Ambiente Único',   desc: 'La Academia te espera con el calor de su hinchada inigualable en cada partido.' },
    { icon: '🎫', title: 'Entrada Digital',  desc: 'Tu ticket llega directo a tu celular con código QR. Sin filas, sin demoras.' },
    { icon: '⚽', title: 'Alta Competencia', desc: 'Liga Profesional, Copa Argentina, Copa Libertadores. Racing juega siempre en grande.' }
  ];
}
