import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { EvmDashboard } from './evm-dashboard';

describe('EvmDashboard', () => {
  let component: EvmDashboard;
  let fixture: ComponentFixture<EvmDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EvmDashboard],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(EvmDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
