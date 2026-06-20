import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';
import { Router, provideRouter, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: any;
  let routerSpy: any;

  beforeEach(async () => {
    // Create spies for the dependencies
    authServiceSpy = { login: vi.fn() };
    routerSpy = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [LoginComponent], // Standalone component
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: {} },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should not call login if username or password is empty', () => {
    component.username = '';
    component.password = '';
    component.onLogin();

    expect(authServiceSpy.login).not.toHaveBeenCalled();
    expect(component.isLoading).toBe(false);
  });

  it('should call authService.login and navigate on success', () => {
    component.username = 'testuser';
    component.password = 'password123';
    
    // Mock successful login returning a fake token
    authServiceSpy.login.mockReturnValue(of('fake-jwt-token'));

    component.onLogin();

    expect(component.isLoading).toBe(false);
    expect(authServiceSpy.login).toHaveBeenCalledWith('testuser', 'password123');
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/feed']);
  });

  it('should handle login error and display error message', () => {
    component.username = 'testuser';
    component.password = 'wrongpassword';
    
    // Mock failed login
    const errorResponse = { error: 'Invalid credentials' };
    authServiceSpy.login.mockReturnValue(throwError(() => errorResponse));

    component.onLogin();

    expect(component.isLoading).toBe(false);
    expect(authServiceSpy.login).toHaveBeenCalledWith('testuser', 'wrongpassword');
    expect(component.error).toBe('Invalid credentials');
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });
});
