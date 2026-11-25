import { Component, OnInit } from '@angular/core';
import { ApiService } from 'src/app/core/services//api.service';
import { AuthService } from './services/auth.service';
import { Router } from '@angular/router';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  loading: boolean | undefined;

  constructor(
    private api: ApiService,
    private auth: AuthService,
    private router: Router,
    themeService: ThemeService
  ) {
    themeService.initialize();
  }

  ngOnInit() {
    if (this.auth.getAccessToken()) {
      this.getUsers();
    }
  }

  getUsers() {
    this.loading = true;

    this.api.post('auth/access-token/users', { 'access_token': this.auth.getAccessToken() }).subscribe((response: any) => {
      this.loading = false;

      this.auth.setUser(response);
    }, error => {
      this.loading = false;

      this.router.navigate(['logout']);
    });
  }

}
