import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { ResourceHistogram } from './resource-histogram';

describe('ResourceHistogram', () => {
  let component: ResourceHistogram;
  let fixture: ComponentFixture<ResourceHistogram>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResourceHistogram],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ResourceHistogram);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
